namespace SadieTester.Networking.Packets;

public interface INetworkPacketReader
{
    string ReadString();
    int ReadInteger();
    bool ReadBool();
}