namespace SadieTester.Networking.Packets.Writers.Rooms;

public class RoomUserChat : NetworkPacketWriter
{
    public RoomUserChat(string message, int bubbleId)
    {
        WriteShort(1314);
        WriteString(message);
        WriteInteger(bubbleId);
    }
}