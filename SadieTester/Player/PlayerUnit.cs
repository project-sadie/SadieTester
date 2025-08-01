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
        websocketClient.MessageReceived.Subscribe(OnMessageReceived);
        websocketClient.DisconnectionHappened.Subscribe(OnDisconnect);
        websocketClient.ReconnectionHappened.Subscribe(OnReconnect);
        
        await Task.Run(() =>
        {
            websocketClient.Start();
        });
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
        Log.Logger.Error($"{player.Username} disconnected: {info.Type.ToString()} {info.CloseStatusDescription}");
        playerRepository.PlayerUnits.TryRemove(player.Id, out var _);
    }

    public IWebsocketClient Client => websocketClient;
    
    public bool TrySendHandshake()
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
        
        websocketClient.Send(new ClientVersionWriter().GetAllBytes());
        websocketClient.Send(new SecureLoginWriter(token.Token).GetAllBytes());

        return true;
    }

    private async void OnMessageReceived(ResponseMessage message)
    {
        foreach (var packet in DecodePacketsFromBytes(message.Binary))
        {
            await packetHandler.HandleAsync(this, packet);
        }
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

            LoadRoom(roomId);
            NavigatorSearchResults.Clear();
        }
        else
        {
            CreateRoom();
        }
    }

    public void LoadRoom(int roomId)
    {
        RoomSession = null;
        websocketClient.Send(new LoadRoomWriter(roomId).GetAllBytes());
    }

    public async Task SayInRoomAsync(string message, int bubble = 0)
    {
        _ = Task.Run(async () =>
        {
            websocketClient.Send(new RoomUserStartTypingWriter().GetAllBytes());
            await Task.Delay(message.Length * 90);
            websocketClient.Send(new RoomUserChat(message, bubble).GetAllBytes());
            await Task.Delay(100);
            websocketClient.Send(new RoomUserStopTypingWriter().GetAllBytes());
        });
    }

    public void WalkTo(Point point)
    {
        websocketClient.Send(new RoomUserWalkWriter(point.X, point.Y).GetAllBytes());
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
                        CreateRoom();
                    }
                    else
                    {
                        var roomId = NavigatorSearchResults
                            .OrderBy(x => x.UsersNow)
                            .First()
                            .Id;

                        LoadRoom(roomId);
                        NavigatorSearchResults.Clear();
                    }
                }
                else if (SecureRandom.OneIn(30))
                {
                    CreateRoom();
                }
            }
            
            NoRoomTicks++;
            return;
        }

        NoRoomTicks = 0;
        
        if (SecureRandom.OneIn(8))
        {
            WalkTo(RoomSession.GetRandomPoint());
        }
        else if (SecureRandom.OneIn(4) && RoomSession.Users.Count > 2)
        {
            LookToPoint(RoomSession.GetRandomUser(player.Id).Position);
        }
        else if (SecureRandom.OneIn(60))
        {
            websocketClient.Send(new RoomUserDanceWriter(GlobalState.Random.Next(1, 4)).GetAllBytes());
        }
        else if (SecureRandom.OneIn(70))
        {
            CreateRoom();
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

    private void CreateRoom()
    {
        var faker = new Faker<CreateRoomData>()
            .RuleFor(u => u.Name, f => f.Address.StreetAddress())
            .RuleFor(u => u.Description, f => f.Lorem.Sentences(5))
            .RuleFor(u => u.Layout, f => ModelSelector.GetRandomModel());

        var data = faker.Generate();
        
        websocketClient.Send(new CreateRoomWriter(data).GetAllBytes());
    }

    public List<NavigatorSearchRoomResult> NavigatorSearchResults = [];
    
    private async Task WaitForNavigatorResultsAsync(TimeSpan timeOut)
    {
        if (NavigatorSearchResults.Any())
        {
            return;
        }
        
        var started = DateTime.Now;
        websocketClient.Send(new NavigatorSearchWriter().GetAllBytes());

        while (NavigatorSearchResults.Count < 1 && ReceivedNavigatorSearchResults < started)
        {
            if ((DateTime.Now - started).TotalSeconds > timeOut.TotalSeconds)
            {
                return;
            }
            
            await Task.Delay(100);
        }
    }

    public void LookToPoint(Point point)
    {
        websocketClient.Send(new RoomUserLookToPointWriter(point).GetAllBytes());
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

    public void Pong()
    {
        websocketClient.Send(new PlayerPongWriter().GetAllBytes());
    }
}