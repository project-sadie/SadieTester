using System.Collections.Concurrent;
using SadieTester.Helpers;

namespace SadieTester.Player;

public class PlayerRepository : IAsyncDisposable
{
    public ConcurrentDictionary<long, PlayerUnit> PlayerUnits { get; } = new();

    public async Task AddPlayerAsync(long id, PlayerUnit unit, CancellationToken token)
    {
        if (!PlayerUnits.TryAdd(id, unit))
            throw new Exception($"Player {id} already exists.");

        // Start its autonomous async loop
        _ = Task.Run(() => RunPlayerLoopAsync(unit, token), token);
    }

    private async Task RunPlayerLoopAsync(PlayerUnit player, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                if (now - player.LastCheck >= TimeSpan.FromSeconds(10))
                {
                    await player.RunPeriodicChecksAsync();
                    player.LastCheck = now;
                }

                await Task.Delay(50, token);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Player loop error: {ex}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var player in PlayerUnits.Values)
        {
            await player.DisposeAsync();
        }

        PlayerUnits.Clear();
    }
}