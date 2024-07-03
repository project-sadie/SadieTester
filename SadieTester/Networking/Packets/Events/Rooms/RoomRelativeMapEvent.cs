using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomRelativeMap)]
public class RoomRelativeMapEvent : INetworkPacketEvent
{
    public required int Something { get; set; }
    public required int MapSize { get; set; }
    
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        return Task.CompletedTask;
    }
}