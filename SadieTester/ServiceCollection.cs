using System.Collections.Concurrent;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SadieTester.Database;
using SadieTester.Database.Mappers;
using SadieTester.Networking.Packets;
using SadieTester.Networking.Packets.Events;
using SadieTester.Player;
using Websocket.Client;

namespace SadieTester;

public static class ServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        var host = config.GetValue<string>("Networking:Host");
        var port = config.GetValue<int>("Networking:Port");
        var useWss = config.GetValue<bool>("Networking:UseWss");
        
        serviceCollection.AddSingleton<PlayerRepository>();
        serviceCollection.AddTransient<WebsocketClient>(provider => new WebsocketClient(new Uri($"{(useWss ? "ws":"wss")}://{host}:{port}")));
        serviceCollection.AddTransient<PlayerUnit>();

        serviceCollection.AddSingleton(provider => new ConcurrentDictionary<int, INetworkPacketEvent>
        {
            [2491] = new SecureLoginOkEvent(),
        });
        
        serviceCollection.AddSingleton<INetworkPacketHandler, ClientPacketHandler>();
        
        var connectionString = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("Default connection string is missing");
        }
        
        serviceCollection.AddDbContext<SadieContext>(options =>
        {
            options.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion, mySqlOptions =>
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                ));
            
            options.UseSnakeCaseNamingConvention();
        });
        
        serviceCollection.AddSingleton<PlayerProfile>();
        
        serviceCollection.AddSingleton(provider => new MapperConfiguration(c =>
        {
            c.AddProfile(provider.GetRequiredService<PlayerProfile>());
        }).CreateMapper());
    }
}