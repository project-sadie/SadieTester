using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomEnterError)]
public class RoomEnterErrorEvent : INetworkPacketEvent
{
    public async Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        var errorCode = reader.ReadInteger();

        if (errorCode == 1)
        {
            if (SecureRandom.OneIn(4))
            {
                await playerUnit.CreateRoomAsync();
            }
            else
            {
                playerUnit.NoRoomTicks = 3;
            }
        }
    }
}