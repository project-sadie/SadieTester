using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using SadieTester.Networking.Packets;
using SadieTester.Player;
using Websocket.Client;

namespace SadieTester.Database.Mappers;

public class PlayerProfile : Profile
{
    public PlayerProfile(IServiceProvider provider, PlayerRepository playerRepository)
    {
        CreateMap<PlayerTrimmedDown, PlayerUnit>()
            .ConstructUsing(x => new PlayerUnit(x,
                provider.GetRequiredService<WebsocketClient>(),
                provider.GetRequiredService<INetworkPacketHandler>(),
                playerRepository));
    }
}