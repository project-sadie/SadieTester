using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using CommandLine;
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
        
        [Option('r', "ramp", Required = false, HelpText = "Enable endless ramp-up mode.")]
        public bool RampUp { get; set; }
    }
    
    private static PlayerRepository? _playerRepository;
    
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("You must provide at least one option. Use --help to see available options.");
            return;
        }
        
        var result = Parser.Default.ParseArguments<Options>(args);

        if (result.Tag == ParserResultType.NotParsed &&
            result.Errors.Any(e => e is HelpRequestedError or VersionRequestedError))
        {
            return;
        }
        
        var maxPlayerCount = 20_000;
        var useKeyConfirm = false;
        var sleepTime = 500;
        var confirmLaunch = false;
        var useRampUp = false;
        var quiet = false;
        
        const int rampLevel = 4;
        const int maxLoadingPlayers = 6;
        
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
                
                if (o.SleepBetweenPlayers > 0)
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

                if (o.RampUp)
                {
                    useRampUp = true;
                }
            });
        
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        AppDomain.CurrentDomain.ProcessExit += OnClose;

        static void OnClose(object? sender, EventArgs e)
        {
            _playerRepository?
                .DisposeAsync()
                .AsTask()
                .GetAwaiter()
                .GetResult();
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
            .UseSerilog((hostContext, _, logger) => logger.WriteTo.Console(theme: CustomTheme.WithBackgrounds()).MinimumLevel.Verbose().MinimumLevel.Override("Microsoft", LogEventLevel.Warning))
            .Build();

        var services = host.Services;
        var mapper = services.GetRequiredService<IMapper>();

        _playerRepository = services.GetRequiredService<PlayerRepository>();

        if (confirmLaunch)
        {
            Log.Logger.Warning("Press ANY key to start loading mock users...");
            Console.ReadKey(true);
        }

        var loaded = 0;
        var excludedIds = new List<long>();
        
        while (maxPlayerCount == 0 || loaded < maxPlayerCount)
        {
            var player = await GetPlayerAsync(services.GetRequiredService<MessageQueueWrapper>());

            if (player == null)
            {
                Log.Logger.Warning("Failed to fetch a player, sleeping before retrying");
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }

            loaded++;

            if (loaded % 10 == 0)
            {
                Console.Title = $"{loaded:N0} players loaded";
            }
            
            var playerUnit = mapper.Map<PlayerUnit>(player);
            
            playerUnit.LastCheck = DateTime.Now.AddMilliseconds(Random.Shared.Next(-2000, 2000));
            playerUnit.LastPong  = DateTime.Now.AddMilliseconds(Random.Shared.Next(-4000, 4000));
            
            await _playerRepository.AddPlayerAsync(player.Id, playerUnit, CancellationToken.None);

            excludedIds.Add(player.Id);

            await playerUnit.ConnectAsync();

            if (!await playerUnit.TrySendHandshakeAsync())
            {
                Log.Logger.Error("Cant send handshake");
                continue;
            }

            _ = playerUnit.WaitForAuthenticationAsync(
                onSuccess: async () =>
                {
                    var pCount = _playerRepository.PlayerUnits.Count;
                    if (pCount % 5 == 0)
                        Log.Logger.Information($"{pCount:N0} players have been loaded");
                },
                onTimeout: () =>
                {
                    _playerRepository.PlayerUnits.TryRemove(player.Id, out _);
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
            else if (useRampUp)
            {
                const int limit = 900;
                
                var deduct = (loaded * rampLevel) < limit ? (loaded * rampLevel) : limit;
                
                await Task.Delay(1_000 - deduct);
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

        async Task<PlayerTrimmedDown?> GetPlayerAsync(MessageQueueWrapper mq)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var item = mq.BasicGet("players");
                
                if (item == null)
                {
                    return null;
                }
                
                var message = Encoding.Default.GetString(item.Body.ToArray());
                var player = JsonSerializer.Deserialize<PlayerTrimmedDown>(message, PlayerJsonOptions);

                if (sw.Elapsed.TotalSeconds > 3)
                {
                    Log.Logger.Warning($"Took {sw.Elapsed.TotalSeconds} seconds to retrieve a player from the server :(");
                }

                return player;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"An error occurred while retrieving player data: {ex}");
                return null;
            }
        }
    }
    
    private static readonly JsonSerializerOptions PlayerJsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    private static async Task WaitForLessThanLoading(int maxLoading)
    {
        var sw = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(30);

        while (_playerRepository!.PlayerUnits.Values.Count(x => !x.HasAuthenticated) >= maxLoading)
        {
            if (sw.Elapsed > timeout)
            {
                Log.Warning("Loading wait timeout; forcing continuation");
                break;
            }

            await Task.Delay(150);
        }

        Log.Logger.Debug($"Waited {sw.Elapsed.TotalSeconds:F2}s for loading to drop.");
    }
}