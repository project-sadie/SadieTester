namespace SadieTester.Networking.Packets.Writers.Handshake;

public class ClientVersionWriter : NetworkPacketWriter
{
    public ClientVersionWriter()
    {
        WriteShort(4000);
    }
}