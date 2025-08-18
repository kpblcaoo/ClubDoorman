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

// IMPORTANT: set env vars BEFORE Host.CreateApplicationBuilder so configuration picks them up.
// Telegram token validation in Telegram.Bot expects pattern like <digits>:<35 chars>.
// Use an obviously fake placeholder that still matches the structural format to satisfy validation
// but is unlikely to trip secret scanners (contains PLACEHOLDER marker, zeros, underscores).
const string ValidPlaceholderToken = "000000000:PLACEHOLDER_PLACEHOLDER_PLACEHOLD"; // 9 digits + ':' + 35 chars
// Force override even if variable already set (tests might export https://api.telegram.org etc.)
Environment.SetEnvironmentVariable("DOORMAN_BOT_API", ValidPlaceholderToken);
Environment.SetEnvironmentVariable("DOORMAN_ADMIN_CHAT", "123456789");
// Disable external AI calls by providing local dummy key
Environment.SetEnvironmentVariable("DOORMAN_OPENROUTER_API", "baseline-dummy-key");
Environment.SetEnvironmentVariable("DOORMAN_TEXT_MENTION_FILTER_ENABLE", "true"); // enable link filter for scenarios
Environment.SetEnvironmentVariable("DOORMAN_GOLDEN_BASELINE", "1"); // signal DI to use dummy Telegram client
// Fast-mode / scenario tuning
Environment.SetEnvironmentVariable("DOORMAN_EMOJI_VIOLATIONS_BEFORE_BAN", "2"); // allow observing escalation if needed
Environment.SetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS", "900000050"); // banlist scenario user

// Redirect data writes (approved users, etc.) into baseline sandbox directory (created after build dir known)
var baselineDataRoot = Path.Combine(assemblyDir, "baseline-data");
Directory.CreateDirectory(baselineDataRoot);
Environment.SetEnvironmentVariable("DOORMAN_DATA_ROOT", baselineDataRoot);
Console.WriteLine($"[DEBUG] DOORMAN_DATA_ROOT set to {baselineDataRoot}");

var builder = Host.CreateApplicationBuilder(args);

// Force logging flags (environment-independent)
builder.Services.PostConfigure<LoggingFlagsOptions>(o =>
{
    o.GoldenMasterEnabled = true;
    o.GoldenSampleRate = 1.0;
    // Place snapshots inside the baseline project to avoid cluttering repo root
    var baselineProjectDir = Path.Combine(baselineProjectRoot, "ClubDoorman.Baseline");
    var goldenRoot = Path.Combine(baselineProjectDir, "golden");
    Directory.CreateDirectory(goldenRoot);
    o.GoldenBasePath = goldenRoot;
    o.GoldenDeterministicIds = true;
    o.GoldenFixedDateFolder = "baseline"; // stable folder name
    Console.WriteLine($"[DEBUG] Golden baseline path set to {goldenRoot}");
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
