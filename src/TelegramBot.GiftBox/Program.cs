using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramBot.GiftBox.Application;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;
using TelegramBot.GiftBox.Telegram;

var builder = Host.CreateApplicationBuilder(args);

var giftFileOptions = builder.Configuration
	.GetSection(GiftConfigurationFileOptions.SectionName)
	.Get<GiftConfigurationFileOptions>() ?? new GiftConfigurationFileOptions();
var giftConfigPath = Path.GetFullPath(giftFileOptions.Path, builder.Environment.ContentRootPath);
var calendar = await new GiftConfigurationLoader().LoadAsync(
	giftConfigPath,
	contentRootPath: builder.Environment.ContentRootPath);

var connectionString = builder.Configuration.GetConnectionString("GiftBox");
if (string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException(
		"Set ConnectionStrings__GiftBox using the environment or a secret store.");
}

var telegramOptions = builder.Configuration
	.GetSection(TelegramBotOptions.SectionName)
	.Get<TelegramBotOptions>()
	?? new TelegramBotOptions();
if (string.IsNullOrWhiteSpace(telegramOptions.BotToken))
{
	throw new InvalidOperationException("Set Telegram__BotToken using the environment or a secret store.");
}

builder.Services.Configure<GiftConfigurationFileOptions>(
	builder.Configuration.GetSection(GiftConfigurationFileOptions.SectionName));
builder.Services.Configure<TelegramBotOptions>(
	builder.Configuration.GetSection(TelegramBotOptions.SectionName));
builder.Services.AddSingleton(calendar);
builder.Services.AddSingleton<IGiftCalendarProvider, GiftCalendarProvider>();
builder.Services.AddSingleton<GiftConfigurationLoader>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

builder.Services.AddDbContext<GiftBoxDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IGiftCalendarService, GiftCalendarService>();
builder.Services.AddScoped<IGiftConfigurationReloadService, GiftConfigurationReloadService>();
builder.Services.AddScoped<IGiftPresentationService, GiftPresentationService>();
builder.Services.AddScoped<IRedemptionService, RedemptionService>();
builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();

builder.Services.AddSingleton<ReloadConversationStore>();
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
	string.IsNullOrWhiteSpace(telegramOptions.BaseUrl)
		? new TelegramBotClient(telegramOptions.BotToken)
		: new TelegramBotClient(new TelegramBotClientOptions(
			telegramOptions.BotToken,
			telegramOptions.BaseUrl,
			useTestEnvironment: false)));
builder.Services.AddSingleton<TelegramUpdateHandler>();
builder.Services.AddHostedService<DatabaseInitializationService>();
builder.Services.AddHostedService<TelegramPollingService>();

var host = builder.Build();

await host.RunAsync();
