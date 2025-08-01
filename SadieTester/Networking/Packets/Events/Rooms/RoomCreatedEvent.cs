using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomCreated)]
public class RoomCreatedEvent : INetworkPacketEvent
{
    public async Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        await playerUnit.LoadRoomAsync(reader.ReadInteger());
    }
}