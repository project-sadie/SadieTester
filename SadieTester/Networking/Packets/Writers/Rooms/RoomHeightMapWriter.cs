using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomHeightMapWriter : NetworkPacketWriter
{
    public RoomHeightMapWriter()
    {
        WriteShort(ServerPacketIds.RoomHeightmap);
    }
}