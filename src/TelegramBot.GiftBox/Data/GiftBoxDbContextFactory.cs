using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TelegramBot.GiftBox.Data;

public sealed class GiftBoxDbContextFactory : IDesignTimeDbContextFactory<GiftBoxDbContext>
{
    public GiftBoxDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__GiftBox")
            ?? "Host=localhost;Database=telegrambot_giftbox";

        var options = new DbContextOptionsBuilder<GiftBoxDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GiftBoxDbContext(options);
    }
}
