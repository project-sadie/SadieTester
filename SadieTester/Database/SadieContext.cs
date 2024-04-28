using Microsoft.EntityFrameworkCore;
using SadieTester.Database.Models;

namespace SadieTester.Database;

public class SadieContext(DbContextOptions<SadieContext> options) : DbContext(options)
{
    public DbSet<Models.Player> Players { get; init; }
    public DbSet<PlayerSsoToken> PlayerSsoToken { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Player>().ToTable("players");
        modelBuilder.Entity<PlayerSsoToken>().ToTable("player_sso_tokens");


        modelBuilder.Entity<Models.Player>()
            .HasMany(c => c.Tokens);
    }
}