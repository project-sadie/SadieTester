using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomUserStartTypingWriter : NetworkPacketWriter
{
    public RoomUserStartTypingWriter()
    {
        WriteShort(ServerPacketIds.RoomUserStartTyping);
    }
}