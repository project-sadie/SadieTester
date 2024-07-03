using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers;

public class PlayerPongWriter : NetworkPacketWriter
{
    public PlayerPongWriter()
    {
        WriteShort(ServerPacketIds.PlayerPong);
        WriteInteger(0);
    }
}