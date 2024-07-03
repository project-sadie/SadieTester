using System.Drawing;
using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;
using SadieTester.Rooms;

namespace SadieTester.Networking.Packets.Events.Rooms.Users;

[PacketId(EventHandlerIds.RoomUserData)]
public class RoomUserDataEvent : INetworkPacketEvent
{
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        if (playerUnit.RoomSession == null)
        {
            return Task.CompletedTask;
        }

        playerUnit.RoomSession.Users.Clear();
        
        var userCount = reader.ReadInteger();
        
        for (var i = 0; i < userCount; i++)
        {
            var id = reader.ReadInteger();
            var username = reader.ReadString();
            var motto = reader.ReadString();
            var figureCode = reader.ReadString();
            reader.ReadInteger();
            var x = reader.ReadInteger();
            var y = reader.ReadInteger();
            var z = double.Parse(reader.ReadString());
            var direction = reader.ReadInteger();
            reader.ReadInteger(); // user type
            reader.ReadString();
            reader.ReadInteger();
            reader.ReadInteger();
            reader.ReadString();
            reader.ReadString();
            var achievementScore = reader.ReadInteger();
            reader.ReadBool();
            
            var user = new RoomUser
            {
                Id = id,
                Username = username,
                Motto = motto,
                FigureCode = figureCode,
                Position = new Point(x, y),
                PositionZ = z,
                DirectionHead = direction,
                Direction = direction
            };
            
            playerUnit.RoomSession.Users.Add(user);
        }
        
        return Task.CompletedTask;
    }
}