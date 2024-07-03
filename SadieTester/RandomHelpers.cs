namespace SadieTester;

public class RandomHelpers
{
    public static bool A90PercentChance() => GlobalState.Random.Next(1, 100) >= 10;
    public static bool A80PercentChance() => GlobalState.Random.Next(1, 100) >= 20;
    public static bool A70PercentChance() => GlobalState.Random.Next(1, 100) >= 30;
    public static bool A60PercentChance() => GlobalState.Random.Next(1, 100) >= 40;
    public static bool A50PercentChance() => GlobalState.Random.Next(1, 100) >= 50;
    public static bool A40PercentChance() => GlobalState.Random.Next(1, 100) >= 60;
    public static bool A30PercentChance() => GlobalState.Random.Next(1, 100) >= 70;
    public static bool A20PercentChance() => GlobalState.Random.Next(1, 100) >= 80;
    public static bool A10PercentChance() => GlobalState.Random.Next(1, 100) >= 90;
    public static bool A5PercentChance() => GlobalState.Random.Next(1, 100) >= 95;
    public static bool A2PercentChance() => GlobalState.Random.Next(1, 100) >= 98;
    public static bool A1PercentChance() => GlobalState.Random.Next(1, 100) >= 99;
    public static bool A0_5PercentChance() => GlobalState.Random.Next(1, 200) >= 199;

    public static int GetRandomBubbleId()
    {
        return 0;
    }

    public static string GetRandomLocale()
    {
        var locales = new List<string>
        {
            "en", "el", "de", "cz", "es", "fi", "fr"
        };

        return locales[GlobalState.Random.Next(0, locales.Count - 1)];
    }

    public static string GetRandomChatMessage()
    {
        var one2Ten = GlobalState.Random.Next(1, 10);
        
        if (one2Ten > 8)
        {
            var lorem = new Bogus.DataSets.Finance();
            return "React owes me £" + lorem.Amount();
        }
        
        if (one2Ten > 6)
        {
            var lorem = new Bogus.DataSets.Finance();
            return "send me ££ @ " + lorem.BitcoinAddress();
        }
        
        var messages = new List<string>
        {
            "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            "yo that react kid is a skid",
            "at the end of the day",
            "had enough",
            "i look in the rear view",
            "i cant slow down",
            "speed up time",
            "lost my mind",
            "now",
            "buy etherium its going up",
            "oxycodone in my body, but no i don't codone it",
            "999 i need helpppp",
            "habtard is such a legend bruv",
            "didnt think much of that",
            "knife in my chest, cheese on my face",
            "anyone seen that retard react?",
            "yoooo suck dese nuts",
            "these are some new nuts",
            "i went to the golf place but it was shut",
            "got da strap wid da heat, ona seat, with some wet feet",
            "yo call the cops",
            "bro is having a stroke",
            "oxy oxy oxy",
            "who wants to come watch a movie with me?",
            "I swear to god",
            "wtf is happening",
            "the nights are the hardest",
            "under the stars but none of them shining",
            "oh my god",
            "okay then",
        };
        
        return messages[GlobalState.Random.Next(0, messages.Count - 1)];
    }
    
    public static void ParallelWhile(
        ParallelOptions parallelOptions,
        Func<bool> condition,
        Action<ParallelLoopState> body)
    {
        ArgumentNullException.ThrowIfNull(parallelOptions);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(body);

        int workersCount = parallelOptions.MaxDegreeOfParallelism switch
        {
            -1 => Int32.MaxValue, // -1 means unlimited parallelism.
            _ => parallelOptions.MaxDegreeOfParallelism
        };

        Parallel.For(0, workersCount, parallelOptions, (_, state) =>
        {
            while (!state.ShouldExitCurrentIteration)
            {
                if (!condition()) { state.Stop(); break; }
                body(state);
            }
        });
    }
}
