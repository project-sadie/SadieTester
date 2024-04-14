using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events;

public class SecureLoginOkEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        playerUnit.HasAuthenticated = true;
        return Task.CompletedTask;
    }
}