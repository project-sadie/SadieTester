using Sadie.Db.Models.Players;

namespace SadieTester;

public class PlayerTrimmedDown
{
    public long Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public PlayerDataTrimmedDown? Data { get; set; }
    public PlayerAvatarData? AvatarData { get; set; }
    public PlayerNavigatorSettings? NavigatorSettings { get; set; }
    public PlayerGameSettings? GameSettings { get; set; }
    public ICollection<PlayerSsoToken> Tokens { get; init; } = [];
}


public class PlayerDataTrimmedDown
{
    public int Id { get; init; }

    public PlayerTrimmedDown Player { get; init; }

    public long PlayerId { get; init; }

    public int? HomeRoomId { get; set; }

    public int CreditBalance { get; set; }

    public int PixelBalance { get; set; }

    public int SeasonalBalance { get; set; }

    public int GotwPoints { get; set; }

    public int RespectPoints { get; set; }

    public int RespectPointsPet { get; init; }

    public int AchievementScore { get; init; }

    public bool AllowFriendRequests { get; init; }

    public bool IsOnline { get; set; }

    public DateTimeOffset? LastOnline { get; set; }
}
