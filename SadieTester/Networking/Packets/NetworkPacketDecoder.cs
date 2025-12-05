using System.Buffers.Binary;

namespace SadieTester.Networking.Packets;

public class NetworkPacketDecoder
{
    private readonly MemoryStream _buffer = new();

    public List<NetworkPacket> Feed(byte[] frameData)
    {
        _buffer.Position = _buffer.Length; 
        _buffer.Write(frameData);

        _buffer.Position = 0;

        var packets = new List<NetworkPacket>();

        while (true)
        {
            if (_buffer.Length - _buffer.Position < 4)
                break;

            span: 
            Span<byte> lengthSpan = stackalloc byte[4];
            _buffer.Read(lengthSpan);
            int packetLength = BinaryPrimitives.ReadInt32BigEndian(lengthSpan);

            if (_buffer.Length - _buffer.Position < packetLength)
            {
                // not enough data yet, rewind
                _buffer.Position -= 4;
                break;
            }

            // full packet is available
            byte[] payload = new byte[packetLength];
            _buffer.Read(payload);

            short packetId = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan()[..2]);
            byte[] body = payload.AsSpan()[2..].ToArray();

            packets.Add(new NetworkPacket(packetId, body));
        }

        // compact remaining data
        long remaining = _buffer.Length - _buffer.Position;
        if (remaining > 0)
        {
            byte[] leftover = new byte[remaining];
            _buffer.Read(leftover);

            _buffer.SetLength(0);
            _buffer.Write(leftover);
        }
        else _buffer.SetLength(0);

        return packets;
    }
}