using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TelegramBot.GiftBox.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_users",
                columns: table => new
                {
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    language_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "ru"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bot_users", x => x.telegram_id);
                });

            migrationBuilder.CreateTable(
                name: "gifts",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    gift_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    available_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_unlocked_early = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    config_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gift_activation_codes",
                columns: table => new
                {
                    activation_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    gift_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gift_activation_codes", x => x.activation_code);
                    table.ForeignKey(
                        name: "FK_gift_activation_codes_gifts_gift_id",
                        column: x => x.gift_id,
                        principalTable: "gifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "redemptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    gift_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    activation_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_redemptions_bot_users_telegram_id",
                        column: x => x.telegram_id,
                        principalTable: "bot_users",
                        principalColumn: "telegram_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_redemptions_gifts_gift_id",
                        column: x => x.gift_id,
                        principalTable: "gifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gift_activation_codes_gift_id",
                table: "gift_activation_codes",
                column: "gift_id");

            migrationBuilder.CreateIndex(
                name: "IX_gifts_day_number",
                table: "gifts",
                column: "day_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_activation_code",
                table: "redemptions",
                column: "activation_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_gift_id",
                table: "redemptions",
                column: "gift_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_telegram_id",
                table: "redemptions",
                column: "telegram_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gift_activation_codes");

            migrationBuilder.DropTable(
                name: "redemptions");

            migrationBuilder.DropTable(
                name: "bot_users");

            migrationBuilder.DropTable(
                name: "gifts");
        }
    }
}
