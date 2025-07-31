using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomUserStopTypingWriter : NetworkPacketWriter
{
    public RoomUserStopTypingWriter()
    {
        WriteShort(ServerPacketIds.RoomUserStopTyping);
    }
}