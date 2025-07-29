using System.Drawing;
using SadieTester.Networking.Packets;
using SadieTester.Networking.Packets.Writers.Handshake;
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
        Log.Logger.Warning($"Player reconnected: {info.Type.ToString()}");
        playerRepository.PlayerUnits.TryRemove(player.Id, out var _);
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

    public void LoadRoom(int roomId)
    {
        RoomSession = null;
        websocketClient.Send(new LoadRoomWriter(roomId).GetAllBytes());
    }

    public void SayInRoom(string message, int bubbleId)
    {
        websocketClient.Send(new RoomUserChat(message, bubbleId).GetAllBytes());
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

    private async Task CheckRandomnessAsync()
    {
        if (RoomSession == null)
        {
            return;
        }
        
        if (RandomHelpers.A20PercentChance())
        {
            var bubbleId = RandomHelpers.GetRandomBubbleId();
            SayInRoom(RandomHelpers.GetRandomChatMessage(), bubbleId);
            return;
        }
        
        if (RandomHelpers.A10PercentChance())
        {
            WalkTo(RoomSession.GetRandomPoint());
        }
        
        if (RandomHelpers.A30PercentChance() && RoomSession.Users.Count > 3)
        {
            LookToPoint(RoomSession.GetRandomUser(player.Id).Position);
        }
        
        if (RandomHelpers.A1PercentChance())
        {
            websocketClient.Send(new RoomUserDanceWriter(GlobalState.Random.Next(1, 4)).GetAllBytes());
        }
        
        // Signs
        // Sit

        if (RandomHelpers.A0_5PercentChance())
        {
            var randomRoom = GlobalState.Random.Next(1, 9);
            LoadRoom(randomRoom);
        }
    }

    public void LookToPoint(Point point)
    {
        websocketClient.Send(new RoomUserLookToPointWriter(point).GetAllBytes());
    }

    public async ValueTask DisposeAsync()
    {
        if (websocketClient is IAsyncDisposable websocketClientAsyncDisposable)
            await websocketClientAsyncDisposable.DisposeAsync();
        else
            websocketClient.Dispose();
    }
}