namespace SadieTester;

public class RandomHelpers
{
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
        var messages = new List<string>
        {
            "Hey, how's it going?",
            "Lol, that's hilarious 😂",
            "I'll be online in 10 minutes.",
            "Did you see the game last night?",
            "What are you up to this weekend?",
            "BRB, getting coffee ☕",
            "That sounds awesome!",
            "Can't believe it's already Friday.",
            "Anyone up for a quick match?",
            "Just finished the report. Phew!",
            "Good morning, team!",
            "What’s the ETA on the new patch?",
            "Yeah, I totally agree.",
            "Let's sync later today.",
            "I’m stuck on level 9 😭",
            "Okay, sent you the files.",
            "Haha, classic move!",
            "Do you guys use Trello or Notion?",
            "I'll check and get back to you.",
            "Nice! Congrats 🎉",
            "You're muted btw.",
            "Wait... what happened?!",
            "GG everyone!",
            "Meeting link please?",
            "Lunch break, back in 30.",
            "Yup, all good on my end.",
            "Let me know if you need help.",
            "Is that a bug or a feature? 😅",
            "This is why we can’t have nice things.",
            "One sec, compiling...",
            "Awesome job on the demo!",
            "Did you try restarting it?",
            "Happy Friday everyone! 🎉",
            "Same issue here, confirmed.",
            "Let’s take this offline.",
            "No worries at all.",
            "Ping me when you’re free.",
            "Adding that to my to-do list.",
            "Can we push the meeting?",
            "Oh, I totally missed that!",
            "Thanks for the heads up.",
            "Any updates on this?",
            "I’ll deploy the fix now.",
            "That’s above my pay grade 😅",
            "New PR is up, take a look.",
            "Haha, nailed it!",
            "Typing...",
            "Done and done ✅",
            "Mic check 1 2 3",
            "Goodnight everyone!"
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
