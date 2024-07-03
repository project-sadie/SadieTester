namespace SadieTester.Networking.Attributes;

public class PacketIdAttribute(short id) : Attribute
{
    public short Id { get; } = id;
}