using System.Net.WebSockets;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sadie.Db;
using SadieTester.Database.Mappers;
using SadieTester.Networking.Attributes;
using SadieTester.Networking.Packets;
using SadieTester.Player;
using Websocket.Client;

namespace SadieTester;

public static class ServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        var host = config.GetValue<string>("Networking:Host");
        var useWss = config.GetValue<bool>("Networking:UseWss");
        
        serviceCollection.AddSingleton<PlayerRepository>();
        serviceCollection.AddTransient<WebsocketClient>(provider => new WebsocketClient(new Uri($"{(useWss ? "wss":"ws")}://{host}"), () => new ClientWebSocket
        {
        }));
        serviceCollection.AddTransient<PlayerUnit>();

        serviceCollection.Scan(scan => scan
            .FromAssemblyOf<INetworkPacketEvent>()
            .AddClasses(classes => classes.AssignableTo<INetworkPacketEvent>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        var packetHandlerTypeMap = new Dictionary<short, Type>();
        
        foreach(var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attributes = type.GetCustomAttributes(typeof(PacketIdAttribute), false);
            var headerAttribute = attributes.FirstOrDefault();

            if (headerAttribute == null)
            {
                continue;
            }
            
            packetHandlerTypeMap.Add(((PacketIdAttribute) headerAttribute).Id, type);
        }

        serviceCollection.AddSingleton(packetHandlerTypeMap);
        
        serviceCollection.AddSingleton<INetworkPacketHandler, ClientPacketHandler>();
        
        var connectionString = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("Default connection string is missing");
        }
        
        serviceCollection.AddDbContext<SadieDbContext>(options =>
        {
            options.UseMySql(config.GetConnectionString("Default"), MySqlServerVersion.LatestSupportedServerVersion, mySqlOptions =>
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