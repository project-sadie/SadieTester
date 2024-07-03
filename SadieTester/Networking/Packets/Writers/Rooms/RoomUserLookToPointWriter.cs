using System.Drawing;
using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomUserLookToPointWriter : NetworkPacketWriter
{
    public RoomUserLookToPointWriter(Point point)
    {
        WriteShort(ServerPacketIds.RoomUserLookAt);
        WriteInteger(point.X);
        WriteInteger(point.Y);
    }
}