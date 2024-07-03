namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomUserWalkWriter : NetworkPacketWriter
{
    public RoomUserWalkWriter(int x, int y)
    {
        WriteShort(3320);
        WriteInteger(x);
        WriteInteger(y);
    }
}