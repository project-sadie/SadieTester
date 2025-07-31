using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets.Writers;
using SadieTester.Player;

namespace SadieTester.Networking.Packets.Events.Rooms;

[PacketId(EventHandlerIds.RoomHeightMap)]
public class RoomHeightMapEvent : INetworkPacketEvent
{
    public required bool Unknown1 { get; set; }
    public required int WallHeight { get; set; }
    public required string RelativeHeightmap { get; set; }
    
    public Task HandleAsync(PlayerUnit playerUnit, INetworkPacketReader reader)
    {
        var heightMapLines = RelativeHeightmap.Split("\r");
        var sizeY = heightMapLines.Length;
        var sizeX = heightMapLines[0].Length;

        if (playerUnit.RoomSession == null)
        {
            return Task.CompletedTask;
        }

        playerUnit.RoomSession.MapSizeX = sizeX;
        playerUnit.RoomSession.MapSizeY = sizeY;

        Task.Run(async () =>
        {
            await Task.Delay(1800);

            playerUnit.WalkTo(playerUnit.RoomSession.GetRandomPoint());
            await playerUnit.SayInRoomAsync("I just entered the room!", 6);
        });
        
        return Task.CompletedTask;
    }
}