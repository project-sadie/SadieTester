using Sadie.Enums.Game.Rooms;

namespace SadieTester.Player;

public class NavigatorSearchRoomResult
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string OwnerUsername { get; set; }
    public required RoomAccessType AccessType { get; set; }
    public required int UsersNow { get; set; }
    public required int MaxUsers { get; set; }
    public required string Description { get; set; }
}