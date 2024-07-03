using System.Drawing;

namespace SadieTester.Rooms;

public class RoomItem
{
    public int Id { get; set; }
    public Point Position { get; set; }
    public double PositionZ { get; set; }
    public bool FloorItem { get; set; }
    public bool CanWalk { get; set; }
    public bool CanSit { get; set; }
    public bool CanLay { get; set; }
}