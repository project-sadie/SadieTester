using Sadie.Enums.Game.Rooms;
using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Navigator;

[PacketId(EventHandlerIds.NavigatorRooms)]
public class NavigatorSearchResultsEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        reader.ReadString();
        reader.ReadString();

        var iCount = reader.ReadInteger();
        
        for (var i = 0; i < iCount; i++)
        {
            reader.ReadString();
            reader.ReadString();
            reader.ReadInteger();
            reader.ReadBool();
            reader.ReadInteger();

            var jCount = reader.ReadInteger();
            
            for (var j = 0; j < jCount; j++)
            {
                var roomId = reader.ReadInteger(); // room id
                var roomName = reader.ReadString();
                reader.ReadInteger();
                var roomOwnerName = reader.ReadString();
                var accessType = reader.ReadInteger();
                var users = reader.ReadInteger();
                var maxUsers = reader.ReadInteger();
                var description = reader.ReadString();
                reader.ReadInteger();
                reader.ReadInteger();
                reader.ReadInteger();
                reader.ReadInteger();

                var kCount = reader.ReadInteger();
                
                for (var k = 0; k < kCount; k++)
                {
                    reader.ReadString();
                }
                
                reader.ReadInteger();

                if (users < maxUsers)
                {
                    playerUnit.NavigatorSearchResults.Add(
                        new NavigatorSearchRoomResult
                        {
                            Id = roomId,
                            Name = roomName,
                            OwnerUsername = roomOwnerName,
                            AccessType = (RoomAccessType)accessType,
                            UsersNow = users,
                            MaxUsers = maxUsers,
                            Description = description
                        });
                }
            }
        }
        
        return Task.CompletedTask;
    }
}