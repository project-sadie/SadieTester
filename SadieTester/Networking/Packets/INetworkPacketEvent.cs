using SadieTester.Player;

namespace SadieTester.Networking.Packets;

public interface INetworkPacketEvent
{
    Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader);
}