using System.Security.Authentication;
using SadieTester.Networking.Packets;
using SadieTester.Networking.Packets.Writers.Handshake;
using Serilog;
using Websocket.Client;

namespace SadieTester.Player;

public class PlayerUnit(
    Database.Models.Player player, 
    IWebsocketClient websocketClient, 
    INetworkPacketHandler packetHandler) : NetworkPacketDecoder
{
    public async Task ConnectAsync()
    {
        websocketClient.ReconnectTimeout = TimeSpan.FromSeconds(30);
        websocketClient.ReconnectionHappened.Subscribe(info => 
            Console.WriteLine($"Reconnection happened, type: {info.Type}"));

        websocketClient.MessageReceived.Subscribe(OnMessageReceived);
        
        await Task.Run(() =>
        {
            websocketClient.Start();
        });
    }

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

    public async Task<bool> WaitForAuthenticationAsync()
    {
        var started = DateTime.Now;
        
        while (!HasAuthenticated)
        {
            if ((DateTime.Now - started).TotalSeconds > 10)
            {
                return false;
            }
            
            await Task.Delay(500);
        }

        return true;
    }
}