# AGENTS.md --- Steam Advent Gift Telegram Bot

## 1. Project goal

Build a private, single-recipient Telegram bot for a handmade 24-day
Advent calendar.

The recipient receives a physical scratch card containing a unique
activation code, sends that code to the Telegram bot, and receives the
corresponding gift or reveal. A QR code may open the bot with the
activation code in a Telegram deep-link payload.

The bot is intended for one recipient, but its implementation should
remain configurable and easy to modify for future calendars.

## 2. Confirmed technical decisions

-   Language/runtime: C# on **.NET 10**.
-   Telegram integration: `Telegram.Bot`.
-   Database: **PostgreSQL**, hosted separately on the user's local
    network.
-   PostgreSQL access: **Entity Framework Core** with the Npgsql provider.
-   Configuration format: JSON, using `System.Text.Json`.
-   Hosting: `Microsoft.Extensions.Hosting` with dependency injection
    and configuration.
-   Deployment: Docker Compose, with the bot container running beside
    the PostgreSQL service or host on the local network.
-   Telegram transport: long polling. An optional custom Telegram Bot
    API base URL may be configured; use the public API when it is absent.
-   Project and solution name: `TelegramBot.GiftBox`.
-   User-facing messages support Russian, English, and Polish. Russian is the
    default; users can select a language.
-   No SQLite.
-   No web admin UI in the initial version. Keep admin operations
    independent of Telegram so a future Razor/Blazor UI can reuse them.
-   The bot is private and restricted to explicitly allowed Telegram
    user IDs.
-   Gift content must be data-driven. Adding or changing a gift should
    normally require editing JSON, not changing application code.

Use current stable package versions compatible with .NET 10. Do not
invent package APIs: check the installed package version and its
documentation when implementing.

## 3. Core user experience

1.  The recipient opens the bot using `/start`.
2.  The bot explains how to redeem a scratch-card activation code.
3.  The recipient starts code entry with `/code`, sends a code as a
    normal text message, or follows a QR deep link carrying the code.
4.  The bot normalizes and validates the input.
5.  The bot verifies:
    -   the sender is an authorized, active Telegram user;
    -   the activation code exists;
    -   the gift is active;
    -   the gift is available on the current date, unless an
        administrator has explicitly overridden availability;
    -   the gift has not already been activated, unless this is a
        permitted repeat delivery of the already-activated gift.
6.  The bot records redemption atomically in PostgreSQL.
7.  The bot presents the gift with an appropriate message, image, and
    action (if configured).

The bot should give friendly, non-revealing error messages. Do not
reveal whether a guessed activation code belongs to another gift,
another date, or another user.

## 4. Suggested solution structure

Start with one deployable application. Avoid premature microservices or
unnecessary abstractions.

``` text
TelegramBot.GiftBox/
├── AGENTS.md
├── TelegramBot.GiftBox.sln
├── src/
│   └── TelegramBot.GiftBox/
│       ├── TelegramBot.GiftBox.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Configuration/
│       │   ├── GiftCalendarOptions.cs
│       │   ├── GiftDefinition.cs
│       │   ├── GiftPayload.cs
│       │   └── GiftConfigurationLoader.cs
│       ├── Telegram/
│       │   ├── UpdateHandler.cs
│       │   ├── CommandHandler.cs
│       │   └── MessageHandler.cs
│       ├── Services/
│       │   ├── GiftService.cs
│       │   ├── RedemptionService.cs
│       │   ├── AccessControlService.cs
│       │   └── GiftPresentationService.cs
│       ├── Data/
│       │   ├── GiftBoxDbContext.cs
│       │   ├── Entities/
│       │   └── Migrations/
│       └── Application/
│           └── ActivationCode.cs
├── config/
│   ├── gifts.example.json
│   └── gifts.json                 # local/private; do not commit real secrets
├── compose.yaml
├── .env.example
├── images/
└── README.md
```

Keep the first implementation pragmatic. Interfaces are useful at
service/repository boundaries, but do not create an interface for every
class without a reason.

## 5. Gift configuration

`gifts.json` is the editable source for calendar content. PostgreSQL
stores operational state, including redemptions and authorized users.

Example configuration:

``` json
{
  "calendarName": "Наш адвент 🎄",
    "startDate": "2026-12-01",
    "endDate": "2026-12-24",
  "timeZone": "Europe/Warsaw",
    "defaultLanguage": "ru",
    "recipientTelegramIds": [123456789],
    "adminTelegramIds": [987654321],
  "gifts": [
    {
      "id": "day01",
      "day": 1,
      "availableFrom": "2026-12-01T00:00:00+01:00",
    "activationCode": "WINTER-01-X9K2",
    "type": "steamLink",
      "title": "Первый сюрприз!",
      "description": "Начинаем наше маленькое приключение.",
      "image": "images/day01.jpg",
      "payload": {
        "url": "https://store.steampowered.com/app/123456/"
      }
    },
    {
      "id": "day02",
      "day": 2,
      "availableFrom": "2026-12-02T00:00:00+01:00",
    "activationCode": "WINTER-02-A7M4",
    "type": "walletCode",
      "title": "Немного на новые приключения",
      "description": "Пополнение твоего игрового бюджета!",
      "payload": {
        "code": "XXXX-XXXX-XXXX",
        "amount": 50,
        "currency": "PLN",
        "redeemUrl": "https://store.steampowered.com/account/redeemwalletcode"
      }
    },
    {
      "id": "day03",
      "day": 3,
      "availableFrom": "2026-12-03T00:00:00+01:00",
    "activationCode": "WINTER-03-B2Q8",
            "type": "steamCode",
            "title": "Сегодня игра!",
            "description": "Активируй игру в Steam.",
      "payload": {
                "code": "XXXXX-XXXXX-XXXXX"
      }
    }
  ]
}
```

The dates above are illustrative. Use the actual calendar year and dates
supplied by the project owner before creating the real calendar
configuration.

### Configuration rules

-   `id` must be unique and stable (for example, `day01`).
-   `day` must be unique and within the configured calendar date range.
-   Activation codes must be unique and normalized consistently.
-   `type` selects a supported gift renderer/handler.
-   `payload` is type-specific JSON.
-   Validate the complete file at startup and on reload.
-   Reject invalid configuration with a clear error; do not partially
    apply it.
-   Never log activation codes, Steam keys, Wallet Codes, or other gift
    secrets.
-   Keep real `gifts.json` out of source control. Commit only
    `gifts.example.json`.
-   Avoid putting secret values in exception messages or diagnostic
    output.

### Initial gift types

Implement only those needed for the first calendar, but design the
dispatcher so new types can be added without rewriting activation-code
redemption logic.

-   `steamCode`: reveal the purchased Steam game activation code and,
    if configured, the official activation instructions.
-   `steamLink`: show information about a Steam game or other gift that
    has already been purchased and is scheduled for delivery. The bot
    does not control Steam's delivery.
-   `walletCode`: reveal a Steam Wallet Code and provide the configured
    official redemption URL.
-   `surprise`: present the gift title and description as a text-only
    reveal; no type-specific payload fields are required.

Do not assume the bot can redeem Steam codes or automatically add funds
to a Steam account. It only reveals the purchased code and provides the
appropriate link/instructions.

## 6. PostgreSQL responsibilities

Use PostgreSQL for durable operational state, not as the primary
hand-edited gift catalogue.

Use EF Core with the Npgsql provider through `GiftBoxDbContext` for
database access. Manage schema changes with EF Core migrations. Keep
atomic redemption, uniqueness constraints, and transactional gift
synchronization in PostgreSQL.

Suggested tables:

### `bot_users`

-   `telegram_id BIGINT PRIMARY KEY`
-   `display_name TEXT NULL`
-   `language_code TEXT NOT NULL DEFAULT 'ru'`
-   `is_active BOOLEAN NOT NULL DEFAULT TRUE`
-   `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`

### `gifts`

-   `id TEXT PRIMARY KEY`
-   `day_number INTEGER NOT NULL UNIQUE`
-   `gift_type TEXT NOT NULL`
-   `title TEXT NOT NULL`
-   `description TEXT NULL`
-   `payload JSONB NOT NULL DEFAULT '{}'`
-   `available_from TIMESTAMPTZ NULL`
-   `is_unlocked_early BOOLEAN NOT NULL DEFAULT FALSE`
-   `is_active BOOLEAN NOT NULL DEFAULT TRUE`
-   `config_version TEXT NULL`
-   `updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`

### `gift_activation_codes`

-   `activation_code TEXT PRIMARY KEY`
-   `gift_id TEXT NOT NULL REFERENCES gifts(id)`
-   `is_active BOOLEAN NOT NULL DEFAULT TRUE`
-   `created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`

This table supports multiple valid codes for one gift when an
administrator chooses to add an incoming code during `/reload`.

### `redemptions`

-   `id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY`
-   `gift_id TEXT NOT NULL REFERENCES gifts(id)`
-   `telegram_id BIGINT NOT NULL REFERENCES bot_users(telegram_id)`
-   `activation_code TEXT NOT NULL UNIQUE`
-   `redeemed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()`
-   `UNIQUE (gift_id)`

Recipient and administrator ID lists may overlap; users with both roles
can redeem gifts and run administrative commands. Redemption state must
support an administrator's explicit early unlock and safe repeat delivery
to the same recipient without creating a second activation record. If
multiple recipients are enabled later, revisit the uniqueness rules and
use a composite constraint such as `(gift_id, telegram_id)`.

Add indexes only where useful; the primary and unique constraints
already provide indexes for the main lookup paths.

### Synchronization policy

-   Load and validate the entire JSON configuration before touching the
    database.
-   Synchronize gift definitions in a PostgreSQL transaction.
-   Do not delete redemption history during synchronization.
-   Do not silently reset a redeemed gift.
-   Always apply valid updates from the JSON file. For gifts that have
    not been activated, apply the new configuration without an
    interactive conflict.
-   If an activated gift's activation code changes, `/reload` must ask
    the administrator how to handle that code: add the incoming code as
    another code that can activate the gift, or replace the current code
    with the incoming code while keeping the gift marked as activated.
    In the replacement case, the new code re-delivers the gift without
    creating a new activation. Resolve these conflicts before committing
    the synchronized configuration.
-   Apply other editable metadata while preserving activation state.
-   Consider storing a configuration hash/version to detect changes.
-   Do not silently invalidate a printed activation code. The explicit
    activated-gift conflict flow above governs code changes.

## 7. Atomic redemption and concurrency

The activation-code check and redemption must be safe if Telegram delivers
duplicate updates or two messages arrive close together.

-   Normalize activation-code input (trim whitespace; apply a clearly
    documented case policy).
-   Store normalized activation codes directly; these manually created
    codes are identifiers, not security secrets.
-   Resolve the candidate gift from the validated in-memory
    configuration or a securely synchronized lookup.
-   In a single database transaction, verify eligibility and insert the
    redemption record.
-   Rely on database uniqueness constraints as the final concurrency
    guard.
-   If the gift is already activated by this recipient, load its stored
    state and send the gift again without creating a second activation.
-   If the insert conflicts for another reason, report that the gift has
    already been claimed without exposing gift details.
-   Never send the secret payload before the transaction has committed.
-   Consider Telegram update idempotency if the chosen polling/webhook
    implementation can process duplicate updates.

## 8. Access control and administration

-   Restrict recipient interactions to configured recipient Telegram
    user IDs and administrative commands to configured administrator
    Telegram user IDs. The lists may overlap; users with both roles can
    use both command groups.
-   Check authorization on every update, not only on `/start`.
-   Unknown users should receive a generic access-denied response; do
    not disclose calendar details.
-   Keep the bot token and PostgreSQL connection string in environment
    variables or an appropriate local secret store.
-   Do not put secrets in `appsettings.json`, `gifts.example.json`,
    logs, or Git.
-   Read options from their named `appsettings.json` sections and let
    environment variables override file values.
-   Check the required role flag for each command; do not assume a user
    has exactly one role.
-   Any administrative commands (for example `/reload`, `/status`,
    `/unlock`) must require administrator authorization.
-   `/reload` must validate the new configuration fully before replacing
    the active configuration.
-   `/unlock <day>` may set the gift's early-unlock flag. The override
    must be explicit and persisted on the gift; do not silently bypass
    date checks.

For the initial version, avoid building a large admin interface. A
restricted `/reload` command and clear startup validation are
sufficient.

## 9. Date and time handling

-   Use `Europe/Warsaw` as the calendar time zone unless the owner
    specifies otherwise.
-   Store timestamps in PostgreSQL as `TIMESTAMPTZ`.
-   Evaluate gift availability in the configured calendar time zone.
-   Treat `availableFrom` as the earliest allowed local instant.
-   Do not rely on the server machine's local time zone.
-   The calendar start and end dates are configured alongside gift
    definitions. Check each gift's availability before activation;
    allow only an explicit administrator override to unlock it early.
-   Unless the owner specifies otherwise, a gift is available on or
    after its configured availability date, not only on that exact day.

## 10. Telegram interaction

Commands:

-   `/start` --- welcome message and instructions; handle Telegram deep
    link payloads used by QR codes as activation-code input.
-   `/code` --- prompt for an activation code. Also accept `/code CODE`
    and an activation code sent as a normal text message.
-   `/calendar` --- show the current calendar without revealing
    unreleased gift contents.
-   `/status` --- administrator-only recipient progress/status.
-   `/reload` --- administrator-only configuration reload.
-   `/unlock <day>` --- administrator-only early unlock,
    by setting the gift's persisted early-unlock flag.
-   `/language` --- let an authorized user select Russian, English, or Polish.

The primary interaction is sending an activation code as a normal text
message.

Use inline keyboard buttons only when they provide a real action, such
as opening a Steam page or revealing a riddle answer.

Keep messages warm, festive, and concise. All user-facing messages must
be localized in Russian, English, and Polish, with Russian as the default. Keep
source code, identifiers, and code comments in English. Persist the
selected language for each authorized user.

Telegram deep links start the bot with `/start <payload>`; they do not
invoke `/code` directly. QR activation links should use a valid Telegram
start payload, stay within Telegram's payload character and length
limits, and be handled without logging the raw payload or activation
code.

`/reload` validates the complete JSON file before any database changes.
For activated-gift code conflicts, collect the administrator's choices
in chat first, then apply the synchronized configuration atomically.
Admin actions must be implemented independently of the Telegram command
handlers so a future web UI can reuse them.

## 11. Steam-specific handling

-   The account's Steam region/currency is Poland / PLN, as confirmed by
    the project owner.
-   A Steam Wallet gift card purchased through Steam's digital gifting
    flow is generally delivered through Steam and is not a printable
    Wallet Code.
-   For scratch-card redemption, use a separately purchased Steam Wallet
    Code that is valid for the recipient's wallet currency/region.
-   For Wallet Codes, show the code only after successful redemption and
    include the official Steam Wallet redemption page:
    `https://store.steampowered.com/account/redeemwalletcode`
-   Never claim that the bot has activated a code. It only reveals it.
-   Do not hard-code third-party sellers or assume a code is
    region-compatible; verify each purchased code before adding it to
    the calendar.
-   Steam gifts with scheduled delivery are controlled by Steam. The bot
    can announce or explain the gift but cannot trigger or reschedule
    Steam's delivery.

## 12. Logging, privacy, and operational safety

-   Log operational events (startup, config reload success/failure,
    redemption success/failure category).
-   Never log raw Telegram message text when it may contain an activation
    code.
-   Never log Steam Wallet Codes, Steam keys, bot tokens, or database
    credentials.
-   Avoid logging full gift payloads.
-   Use structured logging.
-   Handle Telegram API errors and database connectivity failures
    without exposing stack traces or secrets to the recipient.
-   Keep PostgreSQL backups and protect them as they may contain
    sensitive gift data.
-   Restrict filesystem permissions on `gifts.json`, environment files,
    and image assets.
-   Use HTTPS if deploying a webhook endpoint. Long polling is a
    reasonable simpler starting point if the host can maintain an
    outbound connection.

## 13. Implementation order

Build incrementally and keep the project runnable after each stage.

1.  Create the .NET 10 `TelegramBot.GiftBox` solution and configure
    Generic Host / dependency injection.
2.  Add Telegram.Bot, recipient/admin allow-lists, `/start`, `/code`,
    QR deep-link handling, and language selection.
3.  Add PostgreSQL connectivity with EF Core and the Npgsql provider.
4.  Create and apply EF Core migrations for the database schema.
5.  Define strongly typed JSON models and configuration validation.
6.  Implement gift synchronization from JSON to PostgreSQL.
7.  Implement activation-code normalization and direct code lookup.
8.  Implement atomic activation with a transaction and unique constraints.
9.  Implement gift-type presentation for the initial required types.
10. Add `/calendar`, administrator-only `/status`, `/reload` conflict
    choices, and `/unlock <day>`.
11. Add tests for invalid activation codes, unauthorized users,
    unavailable gifts, duplicate and concurrent activation, invalid
    JSON, configuration reload, and repeat delivery.
12. Add Docker Compose and `.env.example`; document deployment,
    environment variables, backup, and recovery.

## 14. Acceptance criteria for the MVP

The MVP is complete when:

-   The bot starts using configuration and secrets supplied outside
    source control.
-   Only configured recipient accounts can use the calendar, and only
    configured administrator accounts can run admin commands.
-   Invalid activation codes do not reveal gift metadata.
-   A valid activation code for an unavailable gift is not accepted.
-   A valid available activation code activates exactly one gift.
-   Re-sending an activation code safely re-delivers an already
    activated gift to the same recipient without a duplicate activation
    record.
-   Concurrent attempts cannot redeem the same gift twice.
-   A successful redemption is persisted in PostgreSQL.
-   A restart does not lose redemption history.
-   Gifts can be edited through JSON without changing application code.
-   Calendar date range and gift definitions are configured in JSON;
    valid reloads apply updates, with explicit admin choices for changed
    codes belonging to activated gifts.
-   Invalid JSON is rejected without corrupting the currently active
    configuration or database state.
-   Steam codes and keys are never written to logs.
-   The bot provides a clear, friendly response for each expected
    failure case.
-   User-facing messages support Russian (default), English, and Polish.
-   The bot can be run with Docker Compose and long polling, using the
    public Telegram Bot API by default or an optional custom API base URL.

## 15. Guidance for coding agents

When working in this repository:

1.  Read this file before making changes.
2.  Preserve the confirmed stack: .NET 10, Telegram.Bot, PostgreSQL,
    EF Core with the Npgsql provider, and System.Text.Json.
3.  Do not introduce SQLite, Redis, or cloud services unless explicitly
    requested. Docker Compose is explicitly part of this project.
4.  Prefer small, testable changes over speculative framework layers.
5.  Do not invent API signatures. Check the actual package version and
    use APIs supported by it.
6.  Keep secrets out of source control and logs.
7.  Add or update tests for behavior changes.
8.  Update this file and the README when architecture or operational
    behavior changes.
9.  If a requirement is ambiguous, choose the simplest safe behavior and
    document the assumption; ask the owner when the decision affects
    gift availability, security, or irreversible redemption.
10. Never create real gift codes or fabricate Steam credentials. Use
    clearly marked placeholders in examples and tests.
