using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomLoaded)]
public class RoomLoadedEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        playerUnit.InRoom = true;
        playerUnit.RoomSession = new PlayerUnitRoomSession();
        
        return Task.CompletedTask;
    }
}