namespace SadieTester.Player;

public class PlayerRepository(Dictionary<int, PlayerUnit> playerUnits)
{
    public Dictionary<int, PlayerUnit> PlayerUnits { get; } = playerUnits;
}