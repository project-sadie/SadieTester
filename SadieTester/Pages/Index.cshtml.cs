using Microsoft.AspNetCore.Mvc.RazorPages;
using SadieTester.Player;

namespace SadieTester.Pages;

public class IndexModel : PageModel
{
    private readonly PlayerRepository? _repo;

    public List<PlayerView> Players { get; set; } = [];

    public IndexModel(PlayerRepository? repo)
    {
        _repo = repo;
    }

    public void OnGet()
    {
        if (_repo == null)
        {
            return;
        }

        Players = _repo.PlayerUnits.Select(x => new PlayerView
        {
            Id = x.Key,
            Username = x.Value.Player?.Username ?? "(unknown)",
            Authenticated = x.Value.HasAuthenticated,
            LastCheck = x.Value.LastCheck,
            LastPong = x.Value.LastPong
        }).ToList();
    }

    public class PlayerView
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public bool Authenticated { get; set; }
        public DateTime LastCheck { get; set; }
        public DateTime LastPong { get; set; }
    }
}