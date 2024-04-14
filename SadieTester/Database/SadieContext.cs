using Microsoft.EntityFrameworkCore;
using SadieTester.Database.Models;

namespace SadieTester.Database;

public class SadieContext(DbContextOptions<SadieContext> options) : DbContext(options)
{
    public DbSet<Models.Player> Players { get; init; }
    public DbSet<PlayerSsoToken> PlayerSsoToken { get; init; }
}