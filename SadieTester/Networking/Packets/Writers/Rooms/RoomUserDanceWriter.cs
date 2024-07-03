namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomUserDanceWriter : NetworkPacketWriter
{
    public RoomUserDanceWriter(int danceId)
    {
        WriteShort(2080);
        WriteInteger(danceId);
    }
}