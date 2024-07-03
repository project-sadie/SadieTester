namespace SadieTester.Networking.Packets.Writers.Rooms;

public class LoadRoomWriter : NetworkPacketWriter
{
    public LoadRoomWriter(int roomId)
    {
        WriteShort(2312);
        WriteInteger(roomId);
        WriteString("");
    }
}