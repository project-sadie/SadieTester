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
            "Lol, that's hilarious.",
            "I'll be online in 10 minutes.",
            "Did you see the game last night?",
            "What are you up to this weekend?",
            "BRB, getting coffee.",
            "That sounds awesome.",
            "Can't believe it's already Friday.",
            "Anyone up for a quick match?",
            "Just finished the report. Phew.",
            "Good morning, team.",
            "What’s the ETA on the new patch?",
            "Yeah, I totally agree.",
            "Let's sync later today.",
            "I'm stuck on level 9.",
            "Okay, sent you the files.",
            "Haha, classic move.",
            "Do you guys use Trello or Notion?",
            "I'll check and get back to you.",
            "Nice! Congrats.",
            "You're muted, by the way.",
            "Wait... what happened?",
            "GG everyone.",
            "Meeting link please?",
            "Lunch break, back in 30.",
            "Yup, all good on my end.",
            "Let me know if you need help.",
            "Is that a bug or a feature?",
            "This is why we can’t have nice things.",
            "One sec, compiling...",
            "Awesome job on the demo.",
            "Did you try restarting it?",
            "Happy Friday everyone.",
            "Same issue here, confirmed.",
            "Let’s take this offline.",
            "No worries at all.",
            "Ping me when you’re free.",
            "Adding that to my to-do list.",
            "Can we push the meeting?",
            "Oh, I totally missed that.",
            "Thanks for the heads up.",
            "Any updates on this?",
            "I’ll deploy the fix now.",
            "That’s above my pay grade.",
            "New PR is up, take a look.",
            "Haha, nailed it.",
            "Typing...",
            "Done and done.",
            "Mic check 1 2 3.",
            "Goodnight everyone.",
            "On my way.",
            "Let me reboot real quick.",
            "Think we can ship this today?",
            "Hold on, checking the logs.",
            "Alright, I’m jumping into the call now.",
            "Just hopped on.",
            "Let me double-check that.",
            "Who’s hosting the meeting?",
            "Give me a second to set things up.",
            "I'm reviewing it now.",
            "Can someone confirm this?",
            "Almost done on my side.",
            "Let’s keep it simple.",
            "Uploading the new build.",
            "Which channel should we use?",
            "Coffee break before we continue?",
            "I'm reading through the notes.",
            "Let's wrap this up soon.",
            "That took longer than expected.",
            "Ready when you are.",
            "Testing it locally first.",
            "I'm heading out for a bit.",
            "Let me clean this up quickly.",
            "Does anyone remember the login?",
            "All right, let’s try that again."
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
