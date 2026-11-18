# TelegramBot.GiftBox

A private Telegram bot for a data-driven Advent gift calendar. It uses .NET 10,
Telegram.Bot long polling, PostgreSQL, and EF Core migrations. User-facing
messages support Russian, English, and Polish, with Russian as the default.
Gift titles and descriptions are shown as authored in the calendar JSON and are
not translated automatically.

## Configuration

Copy the templates and replace every placeholder before starting the bot:

```sh
cp .env.example .env
cp config/gifts.example.json config/gifts.json
```

Edit `config/gifts.json` with the actual calendar dates, Telegram recipient and
administrator IDs, activation codes, and gift payloads. The example contains
placeholders only. The real calendar file is ignored by Git.

Set these values in `.env`:

- `ConnectionStrings__GiftBox`: PostgreSQL connection string. The database must
	be reachable from the container; PostgreSQL is not included in this Compose
	file.
- `Telegram__BotToken`: token supplied by BotFather.
- `Telegram__BaseUrl`: optional custom Telegram Bot API base URL. Leave empty to
	use the public API.
- `GiftConfiguration__Path`: calendar file path inside the container; defaults
	to `config/gifts.json`.

Non-secret defaults can be set in `appsettings.json` and overridden for local
development in `appsettings.Development.json`. Environment variables have the
highest priority, so secrets should be supplied there or through a secret
store.

Do not commit `.env`, `config/gifts.json`, bot tokens, activation codes, Steam
keys, or Wallet Codes.

## Run

Start the bot container:

```sh
docker compose up --build -d
```

The bot applies EF Core migrations and synchronizes gift metadata on startup.
When a changed code belongs to an already activated gift, synchronization waits
for the administrator's `/reload` choices in Telegram. `/unlock <day>` records
an early-unlock flag on the gift. Removing a gift from the JSON deactivates
the gift and its codes while preserving redemption history.

Long polling requires no inbound ports. The bot container must be able to reach
the separately hosted PostgreSQL server and, unless configured otherwise, the
public Telegram Bot API.

## Development

When `DOTNET_ENVIRONMENT=Development`, `appsettings.Development.json` selects
`config/gifts.example.json`. Run from the repository root; bot tokens and the
PostgreSQL connection string still come from environment variables.

Run the test suite with:

```sh
dotnet test TelegramBot.GiftBox.sln --configuration Release
```

EF Core migrations are managed with the repository-pinned local tool:

```sh
dotnet tool restore
dotnet tool run dotnet-ef migrations add <MigrationName> \
	--project src/TelegramBot.GiftBox/TelegramBot.GiftBox.csproj
```
