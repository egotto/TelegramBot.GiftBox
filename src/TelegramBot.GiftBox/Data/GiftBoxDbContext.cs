using Microsoft.EntityFrameworkCore;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data.Entities;

namespace TelegramBot.GiftBox.Data;

public sealed class GiftBoxDbContext(DbContextOptions<GiftBoxDbContext> options)
    : DbContext(options)
{
    public DbSet<BotUser> BotUsers => Set<BotUser>();

    public DbSet<Gift> Gifts => Set<Gift>();

    public DbSet<GiftActivationCode> GiftActivationCodes => Set<GiftActivationCode>();

    public DbSet<Redemption> Redemptions => Set<Redemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BotUser>(entity =>
        {
            entity.ToTable("bot_users");
            entity.HasKey(user => user.TelegramId);
            entity.Property(user => user.TelegramId)
                .HasColumnName("telegram_id")
                .ValueGeneratedNever();
            entity.Property(user => user.DisplayName).HasColumnName("display_name");
            entity.Property(user => user.LanguageCode)
                .HasColumnName("language_code")
                .HasMaxLength(2)
                .HasDefaultValue(LanguageCodes.Default);
            entity.Property(user => user.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            entity.Property(user => user.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Gift>(entity =>
        {
            entity.ToTable("gifts");
            entity.HasKey(gift => gift.Id);
            entity.Property(gift => gift.Id).HasColumnName("id").HasMaxLength(100);
            entity.Property(gift => gift.DayNumber).HasColumnName("day_number");
            entity.HasIndex(gift => gift.DayNumber).IsUnique();
            entity.Property(gift => gift.GiftType).HasColumnName("gift_type").HasMaxLength(40);
            entity.Property(gift => gift.Title).HasColumnName("title").HasMaxLength(300);
            entity.Property(gift => gift.Description).HasColumnName("description");
            entity.Property(gift => gift.Image).HasColumnName("image_path").HasMaxLength(500);
            entity.Property(gift => gift.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb");
            entity.Property(gift => gift.AvailableFrom)
                .HasColumnName("available_from")
                .HasColumnType("timestamp with time zone");
            entity.Property(gift => gift.IsUnlockedEarly)
                .HasColumnName("is_unlocked_early")
                .HasDefaultValue(false);
            entity.Property(gift => gift.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            entity.Property(gift => gift.ConfigVersion)
                .HasColumnName("config_version")
                .HasMaxLength(100);
            entity.Property(gift => gift.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<GiftActivationCode>(entity =>
        {
            entity.ToTable("gift_activation_codes");
            entity.HasKey(code => code.ActivationCode);
            entity.Property(code => code.ActivationCode)
                .HasColumnName("activation_code")
                .HasMaxLength(64);
            entity.Property(code => code.GiftId).HasColumnName("gift_id").HasMaxLength(100);
            entity.Property(code => code.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            entity.Property(code => code.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
            entity.HasIndex(code => code.GiftId);
            entity.HasOne(code => code.Gift)
                .WithMany(gift => gift.ActivationCodes)
                .HasForeignKey(code => code.GiftId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Redemption>(entity =>
        {
            entity.ToTable("redemptions");
            entity.HasKey(redemption => redemption.Id);
            entity.Property(redemption => redemption.Id)
                .HasColumnName("id")
                .UseIdentityAlwaysColumn();
            entity.Property(redemption => redemption.GiftId).HasColumnName("gift_id").HasMaxLength(100);
            entity.Property(redemption => redemption.TelegramId).HasColumnName("telegram_id");
            entity.Property(redemption => redemption.ActivationCode)
                .HasColumnName("activation_code")
                .HasMaxLength(64);
            entity.HasIndex(redemption => redemption.ActivationCode).IsUnique();
            entity.HasIndex(redemption => redemption.GiftId).IsUnique();
            entity.Property(redemption => redemption.RedeemedAt)
                .HasColumnName("redeemed_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
            entity.HasOne<Gift>()
                .WithMany()
                .HasForeignKey(redemption => redemption.GiftId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BotUser>()
                .WithMany()
                .HasForeignKey(redemption => redemption.TelegramId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
