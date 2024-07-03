namespace SadieTester;

public class GlobalState
{
    public static readonly Random Random = new((int)DateTime.Now.Ticks);
}