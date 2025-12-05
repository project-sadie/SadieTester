using System.Net.WebSockets;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
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
        serviceCollection.AddTransient<WebsocketClient>(provider =>
        {
            var url = new Uri($"{(useWss ? "wss" : "ws")}://{host}");

            Func<ClientWebSocket> factory = () =>
            {
                var ws = new ClientWebSocket();

                ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(60);;

                return ws;
            };

            var client = new WebsocketClient(url, factory)
            {
                IsReconnectionEnabled = true,
                ErrorReconnectTimeout = TimeSpan.FromSeconds(60)
            };

            client.ReconnectTimeout = TimeSpan.FromDays(365);

            return client;
        });

        
        serviceCollection.AddSingleton<IConnectionFactory, ConnectionFactory>(provider => new ConnectionFactory
        {
            Uri = new Uri(config.GetConnectionString("RabbitMq"))
        });

        serviceCollection.AddSingleton<MessageQueueWrapper>();

        
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