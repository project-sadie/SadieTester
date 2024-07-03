namespace SadieTester.Networking.Packets.Writers.Handshake;

public class SecureLoginWriter : NetworkPacketWriter
{
    public SecureLoginWriter(string ssoToken)
    {
        WriteShort(2419);
        WriteString(ssoToken);
        WriteInteger(0);
    }
}