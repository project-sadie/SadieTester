namespace SadieTester.Networking.Packets;

public interface INetworkPacket : INetworkPacketReader
{
    short PacketId { get; }
}