using ClubDoorman.Infrastructure;
using ClubDoorman.Baseline.Golden;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Models.Logging;

// Baseline generation harness: builds DI container from main project, swaps Telegram client, seeds synthetic updates,
// writes Golden Master snapshots then exits. Keeps production Program.cs clean.

// Ensure working directory matches production expectations (relative data/* lookups)
// AppContext.BaseDirectory = .../ClubDoorman.Baseline/bin/Release/net9.0/
var assemblyDir = AppContext.BaseDirectory;
var baselineProjectRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..")); // -> solution root
var mainProjectDir = Path.Combine(baselineProjectRoot, "ClubDoorman");
if (Directory.Exists(mainProjectDir))
{
    Directory.SetCurrentDirectory(mainProjectDir);
}

var builder = Host.CreateApplicationBuilder(args);

// Provide required minimal environment variables for production registrations
Environment.SetEnvironmentVariable("DOORMAN_BOT_API", Environment.GetEnvironmentVariable("DOORMAN_BOT_API") ?? "999999:baseline-token");
Environment.SetEnvironmentVariable("DOORMAN_ADMIN_CHAT", Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT") ?? "123456789");
// Enable link (text mention) filtering so link scenarios produce Delete
Environment.SetEnvironmentVariable("DOORMAN_TEXT_MENTION_FILTER_ENABLE", "true");
// Redirect data writes (approved users, etc.) into baseline sandbox directory
var baselineDataRoot = Path.Combine(AppContext.BaseDirectory, "baseline-data");
Directory.CreateDirectory(baselineDataRoot);
Environment.SetEnvironmentVariable("DOORMAN_DATA_ROOT", baselineDataRoot);
Console.WriteLine($"[DEBUG] DOORMAN_DATA_ROOT set to {baselineDataRoot}");

// Force logging flags (environment-independent)
builder.Services.PostConfigure<LoggingFlagsOptions>(o =>
{
    o.GoldenMasterEnabled = true;
    o.GoldenSampleRate = 1.0;
    // Golden snapshots under solutionRoot/golden
    var goldenRoot = Path.Combine(baselineProjectRoot, "golden");
    o.GoldenBasePath = goldenRoot;
    o.GoldenDeterministicIds = true;
    o.GoldenFixedDateFolder = "baseline"; // stable folder name
});

// Reuse full main registration
builder.Services.AddClubDoorman(builder.Configuration);

// Replace Telegram wrapper with offline stub
builder.Services.AddSingleton<ITelegramBotClientWrapper, BaselineOfflineTelegramBotClientWrapper>();
// Add seeder
builder.Services.AddSingleton<IGoldenBaselineSeeder, GoldenBaselineSeeder>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Baseline");
logger.LogInformation("Golden baseline tool starting");

var appConfig = host.Services.GetRequiredService<IAppConfig>();
logger.LogInformation("Bot config loaded (TokenPrefix={Prefix})", appConfig.BotApi?.Substring(0, Math.Min(6, appConfig.BotApi.Length)) ?? "null");

using var scope = host.Services.CreateScope();
var seeder = scope.ServiceProvider.GetRequiredService<IGoldenBaselineSeeder>();
await seeder.SeedAsync();
logger.LogInformation("Golden baseline generation finished");
