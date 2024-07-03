using System.Drawing;

namespace SadieTester.Rooms;

public class RoomUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Motto { get; set; }
    public required string FigureCode { get; set; }
    public required Point Position { get; set; }
    public required double PositionZ { get; set; }
    public required int DirectionHead { get; set; }
    public required int Direction { get; set; }
    public Dictionary<string, string> StatusMap { get; set; } = [];
}