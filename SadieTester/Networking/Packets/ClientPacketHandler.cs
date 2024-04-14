using System.Collections.Concurrent;
using SadieTester.Player;
using Serilog;

namespace SadieTester.Networking.Packets;

public class ClientPacketHandler(ConcurrentDictionary<int, INetworkPacketEvent> packets) : INetworkPacketHandler
{
    public async Task HandleAsync(PlayerUnit playerUnit, INetworkPacket packet)
    {
        if (!packets.TryGetValue(packet.PacketId, out var packetEvent))
        {
            Log.Logger.Error($"Couldn't resolve packet event for header '{packet.PacketId}'");
            return;
        }

        await ExecuteAsync(playerUnit, packet, packetEvent);
    }

    private async Task ExecuteAsync(PlayerUnit playerUnit, INetworkPacketReader packet, INetworkPacketEvent @event)
    {
        Log.Logger.Debug($"Executing packet '{@event.GetType().Name}'");
        
        try
        {
            await @event.HandleAsync(playerUnit, packet);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.ToString());
        }
    }
}