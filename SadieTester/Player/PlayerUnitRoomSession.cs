using System.Drawing;
using SadieTester.Rooms;
using Serilog;

namespace SadieTester.Player;

public class PlayerUnitRoomSession
{
    public int MapSizeX { get; set; }
    public int MapSizeY { get; set; }
    public string? RelativeHeightMap { get; set; }
    
    public List<RoomUser> Users { get; } = [];
    public List<RoomItem> Items { get; set; } = [];
    public DateTime LoadedAt { get; set; }

    public Point GetRandomPoint()
    {
        var randomX = GlobalState.Random.Next(1, MapSizeX);
        var randomY = GlobalState.Random.Next(1, MapSizeY);

        if (!string.IsNullOrEmpty(RelativeHeightMap) && 
            RelativeHeightMap.Split("\r")[randomY - 1][randomX - 1].ToString().ToLower() == "x")
        {
            Log.Logger.Error("Movement algorithm picked non walkable tile");
        }
        
        return new Point(randomX, randomY);
    }

    public RoomUser GetRandomUser(long excludeId)
    {
        var users = Users.Where(x => x.Id != excludeId).ToList();
        return users[GlobalState.Random.Next(0, users.Count - 1)];
    }
}