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
        playerUnit.RoomSession.LoadedAt = DateTime.Now;

        Task.Run(async () =>
        {
            await Task.Delay(2000);
            playerUnit.WalkTo(playerUnit.RoomSession.GetRandomPoint());
        });
        
        return Task.CompletedTask;
    }
}