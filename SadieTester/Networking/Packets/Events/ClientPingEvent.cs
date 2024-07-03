using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events;

[PacketId(EventHandlerIds.ClientPing)]
public class ClientPingEvent : INetworkPacketEvent
{
    public async Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        playerUnit.Client.Send(new PlayerPongWriter().GetAllBytes());
    }
}