using SadieTester.Player;

namespace SadieTester.Networking.Packets;

public interface INetworkPacketHandler
{ 
    Task HandleAsync(PlayerUnit playerUnit, INetworkPacket packet);
}