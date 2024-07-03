using System.Drawing;
using SadieTester.Rooms;

namespace SadieTester.Player;

public class PlayerUnitRoomSession
{
    public int MapSizeX { get; set; }
    public int MapSizeY { get; set; }
    public string? RelativeHeightMap { get; set; }
    
    public List<RoomUser> Users { get; set; } = [];
    public List<RoomItem> Items { get; set; } = [];
    public DateTime LoadedAt { get; set; }

    public Point GetRandomPoint()
    {
        var randomX = GlobalState.Random.Next(1, MapSizeX);
        var randomY = GlobalState.Random.Next(1, MapSizeY);

        if (!string.IsNullOrEmpty(RelativeHeightMap) && RelativeHeightMap.Split("\r")[randomY - 1][randomX - 1].ToString().ToLower() == "x")
        {
            Console.WriteLine("walking to a closed tile lol");
        }
        
        return new Point(randomX, randomY);
    }

    public RoomUser GetRandomUser(int excludeId)
    {
        var users = Users.Where(x => x.Id != excludeId).ToList();
        return users[GlobalState.Random.Next(0, users.Count - 1)];
    }
}