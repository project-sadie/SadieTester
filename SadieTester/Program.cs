using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SadieTester.Database;
using SadieTester.Player;
using Serilog;
using ServiceCollection = SadieTester.ServiceCollection;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, collection) => ServiceCollection.AddServices(collection, context.Configuration))
    .UseSerilog((hostContext, _, logger) => logger.WriteTo.Console())
    .Build();

var services = host.Services;
var mapper = services.GetRequiredService<IMapper>();
var dbContext = services.GetRequiredService<SadieContext>();
var playerRepo = services.GetRequiredService<PlayerRepository>();

while (true)
{
    Log.Logger.Debug("Fetching player record from the database...");

    var player = await dbContext
        .Players
        .FirstOrDefaultAsync(x => x.Username.StartsWith("mock_"));

    if (player == null)
    {
        Log.Logger.Warning("Ran out of player records to load...");
        break;
    }
    
    Log.Logger.Debug($"Preparing to load player '{player.Username}'");

    var playerUnit = mapper.Map<PlayerUnit>(player);
    
    playerRepo.PlayerUnits.Add(player.Id, playerUnit);

    await playerUnit.ConnectAsync();

    if (!playerUnit.TrySendHandshake())
    {
        break;
    }

    if (!await playerUnit.WaitForAuthenticationAsync())
    {
        playerRepo.PlayerUnits.Remove(player.Id);
        Log.Logger.Error($"Failed to load player '{player.Username}', WaitForAuthentication timeout");
    }
    
    Log.Logger.Debug($"Player '{player.Username}' finished loading!");
    
    Console.ReadKey();
}

Log.Logger.Debug($"Finished loading {playerRepo.PlayerUnits.Count} players");

Console.ReadKey();