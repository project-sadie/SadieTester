namespace SadieTester.Database.Models;

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; }
    public PlayerData Data { get; set; }
    public List<PlayerSsoToken> Tokens { get; set; } = [];
}