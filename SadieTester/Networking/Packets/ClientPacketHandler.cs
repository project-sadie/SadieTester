using Microsoft.Extensions.DependencyInjection;
using SadieTester.Networking.Packets.Events;
using SadieTester.Player;
using Serilog;

namespace SadieTester.Networking.Packets;

public class ClientPacketHandler(
    IServiceProvider serviceProvider,
    Dictionary<short, Type> packetHandlerTypeMap) : INetworkPacketHandler
{
    public async Task HandleAsync(PlayerUnit playerUnit, INetworkPacket packet)
    {
        if (!packetHandlerTypeMap.TryGetValue(packet.PacketId, out var packetEventType))
        {
            // Log.Logger.Error($"Couldn't resolve packet event for header '{packet.PacketId}'");
            return;
        }

        var eventHandler = (INetworkPacketEvent) ActivatorUtilities.CreateInstance(serviceProvider, packetEventType);

        try
        {
            EventSerializer.SetPropertiesForEventHandler(eventHandler, packet);
        }
        catch (IndexOutOfRangeException)
        {
            Log.Logger.Error($"Failed to set properties for event {packetEventType.FullName}");
        }
        
        await ExecuteAsync(playerUnit, eventHandler, packet);
    }

    private async Task ExecuteAsync(PlayerUnit playerUnit, INetworkPacketEvent @event, INetworkPacket packet)
    {
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