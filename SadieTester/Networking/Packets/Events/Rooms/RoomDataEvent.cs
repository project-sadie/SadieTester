using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Networking.Packets.Writers.Rooms;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomData)]
public class RoomDataEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        playerUnit.SafeSendAsync(new RoomHeightMapWriter().GetAllBytes());
        return Task.CompletedTask;
    }
}