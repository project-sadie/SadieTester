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
        foreach (var player in PlayerUnits.Values.Where(x => (DateTime.Now - x.LastCheck).TotalSeconds >= 10))
        {
            await player.RunPeriodicChecksAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var player in PlayerUnits.Values)
        {
            await player.DisposeAsync();
        }
    }
}