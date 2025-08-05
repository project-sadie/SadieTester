using Serilog.Sinks.SystemConsole.Themes;

namespace SadieTester;

public static class CustomTheme
{
    public static AnsiConsoleTheme WithBackgrounds()
    {
        return new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
        {
            [ConsoleThemeStyle.Text] = "\x1b[37m",
            [ConsoleThemeStyle.SecondaryText] = "\x1b[90m",
            [ConsoleThemeStyle.LevelVerbose] = "\x1b[30;46m",
            [ConsoleThemeStyle.LevelDebug] = "\x1b[30;42m",
            [ConsoleThemeStyle.LevelInformation] = "\x1b[30;44m",
            [ConsoleThemeStyle.LevelWarning] = "\x1b[30;43m",
            [ConsoleThemeStyle.LevelError] = "\x1b[37;41m",
            [ConsoleThemeStyle.LevelFatal] = "\x1b[37;45m"
        });
    }
}