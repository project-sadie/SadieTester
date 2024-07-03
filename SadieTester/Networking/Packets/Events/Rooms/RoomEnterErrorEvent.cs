using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomEnterError)]
public class RoomEnterErrorEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        var errorCode = reader.ReadInteger();

        if (errorCode == 1)
        {
            playerUnit.LoadRoom(GlobalState.Random.Next(1, 9));
        }
        
        return Task.CompletedTask;
    }
}