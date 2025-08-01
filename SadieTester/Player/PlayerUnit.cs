using System.Drawing;
using Bogus;
using SadieTester.Networking.Packets;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Networking.Packets.Writers.Handshake;
using SadieTester.Networking.Packets.Writers.Navigator;
using SadieTester.Networking.Packets.Writers.Rooms;
using Serilog;
using Websocket.Client;

namespace SadieTester.Player;

public class PlayerUnit(
    Sadie.Db.Models.Players.Player player, 
    IWebsocketClient websocketClient, 
    INetworkPacketHandler packetHandler,
    PlayerRepository playerRepository) : NetworkPacketDecoder, IAsyncDisposable
{
    public async Task ConnectAsync()
    {
        websocketClient.ReconnectTimeout = TimeSpan.FromSeconds(120);
        websocketClient.MessageReceived.Subscribe(message => OnMessageReceived(message).ConfigureAwait(false));
        websocketClient.DisconnectionHappened.Subscribe(OnDisconnect);
        websocketClient.ReconnectionHappened.Subscribe(OnReconnect);
        
        await Task.Run(() =>
        {
            websocketClient.Start();
        });
    }
    
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public async Task SafeSendAsync(byte[] message)
    {
        await _sendLock.WaitAsync();
        try
        {
            websocketClient.Send(message);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async void OnReconnect(ReconnectionInfo info)
    {
        var type = info.Type.ToString();

        if (type == "Initial")
        {
            return;
        }
        
        Log.Logger.Warning($"Player reconnected: {type}");
    }

    private async void OnDisconnect(DisconnectionInfo info)
    {
        Log.Logger.Error("{Username} disconnected: Type={Type}, CloseStatus={CloseStatus}, CloseDescription={Desc}, Exception={Ex}",
            player.Username,
            info.Type,
            info.CloseStatus,
            info.CloseStatusDescription ?? "(none)",
            info.Exception?.ToString() ?? "(no exception)");
        
        playerRepository.PlayerUnits.TryRemove(player.Id, out var _);
    }

    public IWebsocketClient Client => websocketClient;
    
    public async Task<bool> TrySendHandshakeAsync()
    {
        if (player.Tokens.Count < 1)
        {
            Log.Logger.Warning($"SSO record missing for player {player.Username}");
            return false;
        }
        
        var token = player.Tokens.FirstOrDefault();
        
        if (token == null)
        {
            Log.Logger.Warning($"Player {player.Username} doesn't have any active tokens, skipping load.");
            return false;
        }
        
        await SafeSendAsync(new ClientVersionWriter().GetAllBytes());
        await SafeSendAsync(new SecureLoginWriter(token.Token).GetAllBytes());

        return true;
    }

    private Task OnMessageReceived(ResponseMessage message)
    {
        return Task.Run(async () =>
        {
            foreach (var packet in DecodePacketsFromBytes(message.Binary))
            {
                await packetHandler.HandleAsync(this, packet);
            }
        });
    }

    public bool HasAuthenticated { get; set; }
    public bool InRoom { get; set; }
    public PlayerUnitRoomSession? RoomSession { get; set; }
    public DateTime LastCheck { get; set; }
    public DateTime LastPong { get; set; }
    public DateTime ReceivedNavigatorSearchResults { get; set; }

    public async Task WaitForAuthenticationAsync(Action onSuccess, Action onFail)
    {
        var started = DateTime.Now;
        
        while (!HasAuthenticated)
        {
            if ((DateTime.Now - started).TotalSeconds > 15)
            {
                onFail.Invoke();
                return;
            }
            
            await Task.Delay(100);
        }

        if ((DateTime.Now - started).TotalSeconds > 10)
        {
            Log.Logger.Warning($"Took {(DateTime.Now - started).TotalSeconds} seconds for {player.Username} to login");
        }
        
        onSuccess.Invoke();
    }

    public async Task LoadRandomRoomAsync()
    {
        await WaitForNavigatorResultsAsync(
            TimeSpan.FromSeconds(2));
        
        if (NavigatorSearchResults.Count != 0)
        {
            var roomId = NavigatorSearchResults
                .OrderBy(x => x.UsersNow)
                .First()
                .Id;

            await LoadRoomAsync(roomId);
            NavigatorSearchResults.Clear();
        }
        else
        {
            await CreateRoomAsync();
        }
    }

    public async Task LoadRoomAsync(int roomId)
    {
        RoomSession = null;
        await SafeSendAsync(new LoadRoomWriter(roomId).GetAllBytes());
    }

    public async Task SayInRoomAsync(string message, int bubble = 0)
    {
        _ = Task.Run(async () =>
        {
            await SafeSendAsync(new RoomUserStartTypingWriter().GetAllBytes());
            await Task.Delay(message.Length * 90);
            await SafeSendAsync(new RoomUserChat(message, bubble).GetAllBytes());
            await Task.Delay(100);
            await SafeSendAsync(new RoomUserStopTypingWriter().GetAllBytes());
        });
    }

    public async Task WalkToAsync(Point point)
    {
        await SafeSendAsync(new RoomUserWalkWriter(point.X, point.Y).GetAllBytes());
    }

    public async Task RunPeriodicChecksAsync()
    {
        if (!HasAuthenticated)
        {
            return;
        }

        LastCheck = DateTime.Now;
        await CheckRandomnessAsync();
    }

    private int NoRoomTicks = 0;
    
    private async Task CheckRandomnessAsync()
    {
        if (RoomSession == null)
        {
            if (NoRoomTicks >= 2)
            {
                await WaitForNavigatorResultsAsync(
                    TimeSpan.FromSeconds(2));

                if (NavigatorSearchResults.Count != 0)
                {
                    if (SecureRandom.OneIn(20))
                    {
                        await CreateRoomAsync();
                    }
                    else
                    {
                        var roomId = NavigatorSearchResults
                            .OrderBy(x => x.UsersNow)
                            .First()
                            .Id;

                        await LoadRoomAsync(roomId);
                        NavigatorSearchResults.Clear();
                    }
                }
                else if (SecureRandom.OneIn(30))
                {
                    await CreateRoomAsync();
                }
            }
            
            NoRoomTicks++;
            return;
        }

        NoRoomTicks = 0;
        
        if (SecureRandom.OneIn(8))
        {
            await WalkToAsync(RoomSession.GetRandomPoint());
        }
        else if (SecureRandom.OneIn(4) && RoomSession.Users.Count > 2)
        {
            await LookToPointAsync(RoomSession.GetRandomUser(player.Id).Position);
        }
        else if (SecureRandom.OneIn(60))
        {
            await SafeSendAsync(new RoomUserDanceWriter(GlobalState.Random.Next(1, 4)).GetAllBytes());
        }
        else if (SecureRandom.OneIn(70))
        {
            await CreateRoomAsync();
        }
        else if (SecureRandom.OneIn(5))
        {
            await SayInRoomAsync(RandomHelpers.GetRandomChatMessage());
        }
        else if ((DateTime.Now - RoomSession.LoadedAt).TotalSeconds > 600)
        {
            await LoadRandomRoomAsync();
        }
    }

    private async Task CreateRoomAsync()
    {
        var faker = new Faker<CreateRoomData>()
            .RuleFor(u => u.Name, f => f.Address.StreetAddress())
            .RuleFor(u => u.Description, f => f.Lorem.Sentences(5))
            .RuleFor(u => u.Layout, f => ModelSelector.GetRandomModel());

        var data = faker.Generate();
        
        await SafeSendAsync(new CreateRoomWriter(data).GetAllBytes());
    }

    public List<NavigatorSearchRoomResult> NavigatorSearchResults = [];
    
    private async Task WaitForNavigatorResultsAsync(TimeSpan timeOut)
    {
        if (NavigatorSearchResults.Any())
        {
            return;
        }
        
        var started = DateTime.Now;
        await SafeSendAsync(new NavigatorSearchWriter().GetAllBytes());

        while (NavigatorSearchResults.Count < 1 && ReceivedNavigatorSearchResults < started)
        {
            if ((DateTime.Now - started).TotalSeconds > timeOut.TotalSeconds)
            {
                return;
            }
            
            await Task.Delay(100);
        }
    }

    public async Task LookToPointAsync(Point point)
    {
        await SafeSendAsync(new RoomUserLookToPointWriter(point).GetAllBytes());
    }

    public async ValueTask DisposeAsync()
    {
        if (websocketClient is IAsyncDisposable websocketClientAsyncDisposable)
        {
            await websocketClientAsyncDisposable.DisposeAsync();
        }
        else
        {
            websocketClient.Dispose();
        }
    }

    public async Task PongAsync()
    {
        await SafeSendAsync(new PlayerPongWriter().GetAllBytes());
    }
}