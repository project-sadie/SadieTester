using System.Collections.Concurrent;
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
        
        websocketClient.MessageReceived.Subscribe(async message => 
        {
            try
            {
                await OnMessageReceived(message);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unhandled exception in message received");
            }
        });
        
        websocketClient.DisconnectionHappened.Subscribe(OnDisconnect);
        websocketClient.ReconnectionHappened.Subscribe(OnReconnect);
        
        await websocketClient.Start();
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

    private void OnReconnect(ReconnectionInfo info)
    {
        _ = HandleReconnectAsync(info);
    }

    private async Task HandleReconnectAsync(ReconnectionInfo info)
    {
        if (info.Type == ReconnectionType.Initial)
        {
            return;
        }

        // Log.Logger.Warning($"Player reconnected: {info.Type}");
    }

    private bool IsFatalDisconnect(DisconnectionInfo info)
    {
        var closeStatus = info.CloseStatus;
        var ex = info.Exception;

        if (closeStatus != null)
        {
            return (int)closeStatus switch
            {
                1000 => false,
                1001 or 1002 or 1003 or 1006 or 1011 => true,
                _ => (int)closeStatus >= 4000 && (int)closeStatus <= 4999 || true
            };
        }

        if (ex == null)
        {
            return false;
        }

        if (ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ex.Message.Contains("connection refused", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("connection reset", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDisconnect(DisconnectionInfo info)
    {
        if (IsFatalDisconnect(info))
        {
            Log.Logger.Error("Fatal disconnect for {Username}: CloseStatus={CloseStatus}, Exception={Ex}",
                player.Username, info.CloseStatus, info.Exception?.ToString() ?? "(none)");

            playerRepository.PlayerUnits.TryRemove(player.Id, out _);
        }
        else
        {
            //Log.Logger.Information("Non-fatal disconnect for {Username}: CloseStatus={CloseStatus}", player.Username, info.CloseStatus);
        }
    }

    public IWebsocketClient Client => websocketClient;
    
    public async Task<bool> TrySendHandshakeAsync()
    {
        if (player.Tokens.Count < 1)
        {
            Log.Logger.Warning($"SSO record missing for player {player.Username}");
            return false;
        }
        
        var token = player
            .Tokens
            .FirstOrDefault(x => x.UsedAt == null && x.ExpiresAt > DateTime.Now);
        
        if (token == null)
        {
            Log.Logger.Warning($"Player {player.Username} doesn't have any active tokens, skipping load.");
            return false;
        }
        
        await SafeSendAsync(new ClientVersionWriter().GetAllBytes());
        await SafeSendAsync(new SecureLoginWriter(token.Token).GetAllBytes());

        return true;
    }

    private async Task OnMessageReceived(ResponseMessage message)
    {
        try
        {
            foreach (var packet in DecodePacketsFromBytes(message.Binary))
            {
                await packetHandler.HandleAsync(this, packet);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error processing incoming packets for player {Username}", player.Username);
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
            if ((DateTime.Now - started).TotalSeconds > 12)
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
            var snapshot = NavigatorSearchResults.ToList();
            var roomId = snapshot
                .OrderBy(x => x.UsersNow)
                .First()
                .Id;

            await LoadRoomAsync(roomId);
            NavigatorSearchResults = new ConcurrentBag<NavigatorSearchRoomResult>();
        }
        else if (SecureRandom.OneIn(95))
        {
            await CreateRoomAsync("NO_NAV_RESULTS");
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
            try
            {
                await SafeSendAsync(new RoomUserStartTypingWriter().GetAllBytes());
                await Task.Delay(message.Length * 95);
                await SafeSendAsync(new RoomUserChat(message, bubble).GetAllBytes());
                await Task.Delay(50);
                await SafeSendAsync(new RoomUserStopTypingWriter().GetAllBytes());
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.ToString());
            }
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
                    if (SecureRandom.OneIn(500))
                    {
                        await CreateRoomAsync("ONE_IN_500_CHANCE");
                    }
                    else
                    {
                        var snapshot = NavigatorSearchResults.ToList();
                        var roomId = snapshot.OrderBy(x => x.UsersNow).First().Id;

                        await LoadRoomAsync(roomId);
                        NavigatorSearchResults = [];
                    }
                }
                else if (SecureRandom.OneIn(400))
                {
                    await CreateRoomAsync("ONE_IN_400_CHANCE_BACKUP");
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
        else if (SecureRandom.OneIn(80))
        {
            await SafeSendAsync(new RoomUserDanceWriter(GlobalState.Random.Next(1, 4)).GetAllBytes());
        }
        else if (SecureRandom.OneIn(80))
        {
            await SayInRoomAsync(":sit");
        }
        else if (SecureRandom.OneIn(185))
        {
            await SayInRoomAsync(":about");
        }
        else if (SecureRandom.OneIn(90))
        {
            await SayInRoomAsync(":commands");
        }
        else if (SecureRandom.OneIn(105))
        {
            await SayInRoomAsync(":shutdown");
        }
        else if (SecureRandom.OneIn(105))
        {
            await SayInRoomAsync($":enable {SecureRandom.Next(1, 60)}");
        }
        else if (SecureRandom.OneIn(1000))
        {
            await CreateRoomAsync("1_IN_1000_CHANCE");
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

    private async Task CreateRoomAsync(string reason)
    {
        var faker = new Faker<CreateRoomData>()
            .RuleFor(u => u.Name, f => f.Address.StreetAddress())
            .RuleFor(u => u.Description, f => f.Lorem.Sentences(5))
            .RuleFor(u => u.Layout, f => ModelSelector.GetRandomModel());

        var data = faker.Generate();
        
        Log.Logger.Warning($"Creating room '{data.Name}': {reason} for {player.Username}");
        await SafeSendAsync(new CreateRoomWriter(data).GetAllBytes());
    }

    public ConcurrentBag<NavigatorSearchRoomResult> NavigatorSearchResults = new();
    
    private async Task WaitForNavigatorResultsAsync(TimeSpan timeOut)
    {
        if (NavigatorSearchResults.Any())
        {
            return;
        }
        
        var started = DateTime.Now;
        await SafeSendAsync(new NavigatorSearchWriter().GetAllBytes());

        while (NavigatorSearchResults.IsEmpty)
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
        
        _sendLock.Dispose();
    }

    public async Task PongAsync()
    {
        await SafeSendAsync(new PlayerPongWriter().GetAllBytes());
    }

    public long Id => player.Id;
}