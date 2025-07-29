using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms.Items;

[PacketId(EventHandlerIds.RoomFloorItems)]
public class RoomFloorItemsEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        /*if (playerUnit.RoomSession == null)
        {
            return Task.CompletedTask;
        }

        playerUnit.RoomSession.Items.Clear();
        
        var furnitureOwners = reader.ReadInteger();

        for (var x = 0; x < furnitureOwners; x++)
        {
            reader.ReadInteger();
            reader.ReadString();
        }
        
        var userCount = reader.ReadInteger();
        
        for (var i = 0; i < userCount; i++)
        {
            var id = reader.ReadInteger();
            var assetId = reader.ReadInteger();
            var x = reader.ReadInteger();
            var y = reader.ReadInteger();
            var direction = reader.ReadInteger();
            var z = double.Parse(reader.ReadString());
            reader.ReadString();
            reader.ReadString();
            reader.ReadInteger();
            var objectDataKey = reader.ReadInteger();
            var metaData = "";

            if (objectDataKey == 0)
            {
                metaData = reader.ReadString();
            }
            else if (objectDataKey == 1)
            {
                var dataCount = reader.ReadInteger();

                for (var h = 0; h < dataCount; h++)
                {
                    reader.ReadString();
                    reader.ReadString();
                }
            }

            reader.ReadInteger();
            reader.ReadInteger();
            reader.ReadInteger();
            
            var item = new RoomItem
            {
            };
            
            playerUnit.RoomSession.Items.Add(item);
        }*/
        
        return Task.CompletedTask;
    }
}