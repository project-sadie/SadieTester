using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events;

[PacketId(EventHandlerIds.SecureLogin)]
public class SecureLoginOkEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        playerUnit.HasAuthenticated = true;
        playerUnit.LastCheck = DateTime.Now;
        return Task.CompletedTask;
    }
}