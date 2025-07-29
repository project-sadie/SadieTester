using System.Diagnostics;
using AutoMapper;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sadie.Db;
using SadieTester.Player;
using Serilog;
using Serilog.Events;

namespace SadieTester;

internal static class Program
{
    public class Options
    {
        [Option('p', "players", Required = false, HelpText = "Sets the amount of players to load, if not set will be unlimited.")]
        public int Players { get; set; }
        
        [Option('k', "keyConfirm", Required = false, HelpText = "Wait for a console key press to load the next player, else a sleep will be used..")]
        public bool UseKeyConfirm { get; set; }
        
        [Option('s', "sleep", Required = false, HelpText = "Override the default how long to sleep per player load")]
        public int SleepBetweenPlayers { get; set; }
        
        [Option('i', "instantStart", Required = false, HelpText = "Overrides the need for a key press before players start loading")]
        public bool InstantStart { get; set; }
        
        [Option('q', "quiet", Required = false, HelpText = "Makes things a little quieter")]
        public bool Quiet { get; set; }
    }
    
    private static PlayerRepository? _playerRepository;
    
    public static async Task Main(string[] args)
    {
        var maxPlayerCount = 0;
        var useKeyConfirm = false;
        var sleepTime = 950;
        var confirmLaunch = true;
        var quiet = false;
        var maxLoadingPlayers = 4;
        
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                if (o.Players != 0)
                {
                    maxPlayerCount = o.Players;
                    
                    Log.Logger.Warning($"Players to load set to {o.Players}");
                }

                if (o.UseKeyConfirm)
                {
                    useKeyConfirm = true;
                }
                
                if (o.SleepBetweenPlayers != 0 && o.SleepBetweenPlayers >= 600)
                {
                    sleepTime = o.SleepBetweenPlayers;
                }

                if (o.InstantStart)
                {
                    confirmLaunch = false;
                }

                if (o.Quiet)
                {
                    quiet = true;
                }
            });
        
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        AppDomain.CurrentDomain.ProcessExit += OnClose;

        static async void OnClose(object? sender, EventArgs e)
        {
            await _playerRepository.DisposeAsync();
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Logger.Error(e.ExceptionObject.ToString());
        }
        
        Console.CancelKeyPress += OnClose;

        var host = Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((context, collection) => ServiceCollection.AddServices(collection, context.Configuration))
            .UseSerilog((hostContext, _, logger) => logger.WriteTo.Console().MinimumLevel.Verbose().MinimumLevel.Override("Microsoft", LogEventLevel.Warning))
            .Build();

        var services = host.Services;
        var mapper = services.GetRequiredService<IMapper>();
        var dbContext = services.GetRequiredService<SadieDbContext>();

        _playerRepository = services.GetRequiredService<PlayerRepository>();
        _ = _playerRepository.WorkAsync(CancellationToken.None);

        if (confirmLaunch)
        {
            Log.Logger.Warning("Press ANY key to start loading mock users...");
            Console.ReadKey(true);
        }

        var loaded = 0;
        var excludedIds = new List<long>();
        
        while (maxPlayerCount == 0 || loaded < maxPlayerCount)
        {
            loaded++;

            Console.Title = $"{loaded:N0} players loaded";
            
            var player = await GetPlayerAsync(dbContext, excludedIds);

            if (player == null)
            {
                Log.Logger.Warning("Ran out of player records to load");
                break;
            }

            var playerUnit = mapper.Map<PlayerUnit>(player);
            if (!_playerRepository.PlayerUnits.TryAdd(player.Id, playerUnit))
            {
                continue;
            }

            excludedIds.Add(player.Id);

            await playerUnit.ConnectAsync();

            if (!playerUnit.TrySendHandshake())
            {
                continue;
            }
            
            if (loaded % 5 == 0) { Log.Logger.Debug($"Status update, {loaded} players loaded"); }

            _ = playerUnit.WaitForAuthenticationAsync(() =>
            {
                if (!quiet || useKeyConfirm)
                {
                    Log.Logger.Debug($"Player '{player.Username}' finished loading!");
                }
                
                if (player.Id % 3 == 0) { playerUnit.LoadRoom(GlobalState.Random.Next(1, 9)); }
            }, () =>
            {
                _playerRepository.PlayerUnits.TryRemove(player.Id, out var _);
                Log.Logger.Error($"Failed to load player '{player.Username}', WaitForAuthentication timeout");
            });

            if (loaded > maxLoadingPlayers && _playerRepository.PlayerUnits.Values.Count(x => !x.HasAuthenticated) > maxLoadingPlayers)
            {
                await WaitForLessThanLoading(maxLoadingPlayers);
            }

            if (useKeyConfirm)
            {
                Console.ReadKey(true);
            }
            else
            {
                await Task.Delay(sleepTime);
            }
        }

        Log.Logger.Debug($"Finished loading {_playerRepository.PlayerUnits.Count} players");

        for (var i = 0; i < 100; i++)
        {
            Console.ReadKey();
        }

        return;

        async Task<Sadie.Db.Models.Players.Player?> GetPlayerAsync(SadieDbContext dbContext, ICollection<long> excludedIds)
        {
            return await dbContext
                .Players
                .Include(x => x.Tokens)
                .Include(x => x.Data)
                .Where(x => x.Username.EndsWith(".mock") && !x.Data.IsOnline && !excludedIds.Contains(x.Id))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }
    }

    private static async Task WaitForLessThanLoading(int i)
    {
        var sw = Stopwatch.StartNew();
        
        Log.Logger.Warning($"There are more than {i} players waiting to be loaded, waiting till there is less...");
        
        while (_playerRepository!.PlayerUnits.Values.Count(x => !x.HasAuthenticated) >= i)
        {
            await Task.Delay(1000);
        }
        
        Log.Logger.Warning($"Finished waiting, {Math.Round(sw.Elapsed.TotalSeconds, 3)}s");
    }
}