using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomCreated)]
public class RoomCreatedEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        playerUnit.LoadRoom(reader.ReadInteger());
        return Task.CompletedTask;
    }
}