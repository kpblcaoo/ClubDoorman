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

// Helper method to run one baseline variant (normal + optional media filtering disabled variant)
static async Task RunBaselineVariantAsync(string variantName, bool disableMediaFiltering, string[] args)
{
    var assemblyDirLocal = AppContext.BaseDirectory;
    var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirLocal, "..", "..", "..", ".."));
    var mainProjectDirLocal = Path.Combine(solutionRoot, "ClubDoorman");
    if (Directory.Exists(mainProjectDirLocal))
        Directory.SetCurrentDirectory(mainProjectDirLocal);

    // Base common env
    const string ValidPlaceholderTokenInner = "000000000:PLACEHOLDER_PLACEHOLDER_PLACEHOLD";
    Environment.SetEnvironmentVariable("DOORMAN_BOT_API", ValidPlaceholderTokenInner);
    Environment.SetEnvironmentVariable("DOORMAN_ADMIN_CHAT", "123456789");
    Environment.SetEnvironmentVariable("DOORMAN_OPENROUTER_API", "baseline-dummy-key");
    Environment.SetEnvironmentVariable("DOORMAN_TEXT_MENTION_FILTER_ENABLE", "true");
    Environment.SetEnvironmentVariable("DOORMAN_GOLDEN_BASELINE", "1");
    Environment.SetEnvironmentVariable("DOORMAN_EMOJI_VIOLATIONS_BEFORE_BAN", "2");
    Environment.SetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS", "900000050");

    // Variant-specific media filtering toggle
    if (disableMediaFiltering)
    {
        Environment.SetEnvironmentVariable("DOORMAN_DISABLE_MEDIA_FILTERING", "1");
    }
    else
    {
        Environment.SetEnvironmentVariable("DOORMAN_DISABLE_MEDIA_FILTERING", null); // ensure not set
    }

    // Isolate data root per variant
    var variantDataRoot = Path.Combine(assemblyDirLocal, $"baseline-data-{variantName}");
    Directory.CreateDirectory(variantDataRoot);
    Environment.SetEnvironmentVariable("DOORMAN_DATA_ROOT", variantDataRoot);
    Console.WriteLine($"[DEBUG] ({variantName}) DOORMAN_DATA_ROOT = {variantDataRoot}");

    var builderLocal = Host.CreateApplicationBuilder(args);
    builderLocal.Services.PostConfigure<LoggingFlagsOptions>(o =>
    {
        o.GoldenMasterEnabled = true;
        o.GoldenSampleRate = 1.0;
        var baselineProjectDirLocal = Path.Combine(solutionRoot, "ClubDoorman.Baseline");
        var goldenRoot = Path.Combine(baselineProjectDirLocal, "golden");
        Directory.CreateDirectory(goldenRoot);
        o.GoldenBasePath = goldenRoot;
        o.GoldenDeterministicIds = true;
        // Distinct folder per variant so snapshots coexist
        o.GoldenFixedDateFolder = variantName; // e.g. "baseline" or "baseline_mediaoff"
        Console.WriteLine($"[DEBUG] ({variantName}) Golden path root={goldenRoot}");
    });

    builderLocal.Services.AddClubDoorman(builderLocal.Configuration);
    builderLocal.Services.AddSingleton<ITelegramBotClientWrapper, BaselineOfflineTelegramBotClientWrapper>();
    builderLocal.Services.AddSingleton<IGoldenBaselineSeeder, GoldenBaselineSeeder>();

    using var hostLocal = builderLocal.Build();
    var loggerLocal = hostLocal.Services.GetRequiredService<ILoggerFactory>().CreateLogger($"Baseline[{variantName}]");
    loggerLocal.LogInformation("Golden baseline variant '{Variant}' starting (MediaFilteringDisabled={MediaDisabled})", variantName, disableMediaFiltering);
    var appConfigLocal = hostLocal.Services.GetRequiredService<IAppConfig>();
    loggerLocal.LogInformation("Bot config loaded (TokenPrefix={Prefix})", appConfigLocal.BotApi?.Substring(0, Math.Min(6, appConfigLocal.BotApi.Length)) ?? "null");

    using var scopeLocal = hostLocal.Services.CreateScope();
    var seederLocal = scopeLocal.ServiceProvider.GetRequiredService<IGoldenBaselineSeeder>();
    await seederLocal.SeedAsync();
    loggerLocal.LogInformation("Golden baseline variant '{Variant}' finished", variantName);
}

// Run main baseline
await RunBaselineVariantAsync("baseline", disableMediaFiltering: false, args);

// If user sets env DOORMAN_BASELINE_MEDIA_OFF=1 produce second variant with media filtering disabled
var produceMediaOff = Environment.GetEnvironmentVariable("DOORMAN_BASELINE_MEDIA_OFF");
if (string.Equals(produceMediaOff, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(produceMediaOff, "true", StringComparison.OrdinalIgnoreCase))
{
    await RunBaselineVariantAsync("baseline_mediaoff", disableMediaFiltering: true, args);
}
