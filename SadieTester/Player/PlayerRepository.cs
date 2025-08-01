using System.Collections.Concurrent;

namespace SadieTester.Player;

public class PlayerRepository : IAsyncDisposable
{
    public ConcurrentDictionary<long, PlayerUnit> PlayerUnits { get; } = [];
    
    public async Task WorkAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await RunPeriodicChecksAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            await Task.Delay(1000, token);
        }
    }
    
    private async Task RunPeriodicChecksAsync()
    {
        var now = DateTime.Now;

        foreach (var player in PlayerUnits.Values)
        {
            if ((now - player.LastPong).TotalSeconds >= 15)
            {
                await player.PongAsync();
            }
        }

        var periodicTasks = PlayerUnits.Values
            .Where(player => (now - player.LastCheck).TotalSeconds >= 5)
            .Select(player => player.RunPeriodicChecksAsync());

        await Task.WhenAll(periodicTasks);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var player in PlayerUnits.Values)
        {
            await player.DisposeAsync();
        }
    }
}