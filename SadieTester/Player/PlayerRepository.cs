using System.Collections.Concurrent;
using Serilog;

namespace SadieTester.Player;

public class PlayerRepository : IAsyncDisposable
{
    public ConcurrentDictionary<long, PlayerUnit> PlayerUnits { get; } = [];
    
    public async Task WorkAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var loopStart = DateTime.UtcNow;
            
            try
            {
                await RunPeriodicChecksAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            var elapsed = DateTime.UtcNow - loopStart;

            if (elapsed > TimeSpan.FromSeconds(1))
            {
                Log.Logger.Warning("[WARN] RunPeriodicChecksAsync took too long: {Elapsed:N0} ms", elapsed.TotalMilliseconds);
            }
            else
            {
                await Task.Delay(1000, token);
            }
        }
    }
    
    private async Task RunPeriodicChecksAsync()
    {
        var now = DateTime.UtcNow;

        await Parallel.ForEachAsync(PlayerUnits.Values, new ParallelOptions { MaxDegreeOfParallelism = 100 }, async (player, ct) =>
        {
            try
            {
                if ((now - player.LastPong).TotalSeconds >= 10)
                {
                    await player.PongAsync();
                }

                if ((now - player.LastCheck).TotalSeconds >= 5)
                {
                    await player.RunPeriodicChecksAsync();
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e.ToString());
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var player in PlayerUnits.Values)
        {
            await player.DisposeAsync();
        }
    }
}