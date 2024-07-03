using System.Drawing;
using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;
using SadieTester.Rooms;

namespace SadieTester.Networking.Packets.Events.Rooms.Users;

[PacketId(EventHandlerIds.RoomUserStatus)]
public class RoomUserStatusWriter : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        if (playerUnit.RoomSession == null)
        {
            return Task.CompletedTask;
        }

        var userCount = reader.ReadInteger();
        
        for (var i = 0; i < userCount; i++)
        {
            var id = reader.ReadInteger();
            var x = reader.ReadInteger();
            var y = reader.ReadInteger();
            var z = double.Parse(reader.ReadString());
            var directionHead = reader.ReadInteger();
            var direction = reader.ReadInteger();
            var statusString = reader.ReadString();

            var user = playerUnit
                .RoomSession
                .Users
                .FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                user = new RoomUser
                {
                    Id = id,
                    Username = null,
                    Motto = null,
                    FigureCode = null,
                    Position = default,
                    PositionZ = 0,
                    DirectionHead = 0,
                    Direction = 0
                };
            }
            
            user.Position = new Point(x, y);
            user.PositionZ = z;
            user.DirectionHead = directionHead;
            user.Direction = direction;
                
            var statusParts = statusString.Substring(1).Split("/");

            user.StatusMap = [];
                
            user.StatusMap = statusParts.ToDictionary(
                k => k.Split(" ")[0], v => v.Split(" ")[1]);
        }
        
        return Task.CompletedTask;
    }
}