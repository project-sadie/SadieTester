using SadieTester.Networking.Packets.Events;

namespace SadieTester.Networking.Packets.Writers.Handshake;

public class ClientVersionWriter : NetworkPacketWriter
{
    public ClientVersionWriter()
    {
        WriteShort(ServerPacketIds.ClientVersion);
    }
}