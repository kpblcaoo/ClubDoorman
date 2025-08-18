using System.IO;
using System.Linq;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.GoldenMaster;

/// <summary>
/// Дет тест на стабильность Golden Master снапшотов: проверяем что input/output создаются и детерминированы.
/// </summary>
[Category("golden")]
public class GoldenMasterRecorderTests : TestBase
{
    private static IOptions<LoggingFlagsOptions> CreateFlags(string basePath) => Options.Create(new LoggingFlagsOptions
    {
        GoldenMasterEnabled = true,
        GoldenSampleRate = 1.0, // форсируем выборку
        GoldenBasePath = basePath,
    });

    private static Update CreateSampleUpdate()
    {
        // NOTE: In updated Telegram.Bot library some properties (Type, MessageId) became read-only.
        // We only need deterministic content; leaving MessageId default (0) is fine.
        var message = new Message
        {
            // MessageId omitted (read-only)
            Text = "Hello Test",
            Chat = new Chat { Id = -100123456789, Title = "Test Chat", Type = ChatType.Supergroup },
            From = new User { Id = 555123456, IsBot = false, Username = "sample_user" }
        };
        var update = new Update
        {
            Id = 42,
            // Type is inferred from populated Message property in newer versions; do not set explicitly.
            Message = message
        };
        return update;
    }

    [Test]
    public void Recorder_WritesInputAndOptionalSemantics_AfterV1Removal()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "gm_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var recorder = new GoldenMasterRecorder(CreateFlags(tmpDir), new NullLogger<GoldenMasterRecorder>());
            var update = CreateSampleUpdate();
            var correlation = recorder.TryRecordInput(update, "MessageHandler", update.Message!.Chat.Id, update.Message.From!.Id);
            Assert.That(correlation, Is.Not.Null, "correlationId null - запись не произошла");
            recorder.TryRecordOutput(correlation, new { kind = "test", value = 123 }); // no-op now

            // Проверяем существование input файла и допускаем появление дополнительного .sem.json (семантика)
            var date = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dayDir = Path.Combine(tmpDir, date);
            Assert.That(Directory.Exists(dayDir), Is.True, "Day directory not created");
            var files = Directory.GetFiles(dayDir, correlation + "*.json").OrderBy(f => f).ToArray();
            Assert.That(files.Any(f => f.EndsWith("input.json")), Is.True, "Input file missing");
            // Допускаем 1 файл (только input) или 2 файла (input + sem)
            Assert.That(files.Length == 1 || (files.Length == 2 && files.Any(f => f.EndsWith(".sem.json"))), Is.True,
                $"Unexpected file set: {string.Join(", ", files.Select(Path.GetFileName))}");

            var inputJson = File.ReadAllText(files.First(f => f.EndsWith("input.json")));

            // Повторно вызовем Canonicalize (через второй recorder) и убедимся что содержимое input не меняется при повторной записи с тем же Update
            var recorder2 = new GoldenMasterRecorder(CreateFlags(tmpDir), new NullLogger<GoldenMasterRecorder>());
            var correlation2 = recorder2.TryRecordInput(update, "MessageHandler", update.Message.Chat.Id, update.Message.From.Id);
            Assert.That(correlation2, Is.Not.Null);
            var dateDir2 = Path.Combine(tmpDir, date);
            var input2 = File.ReadAllText(Directory.GetFiles(dateDir2, correlation2 + ".input.json").Single());

            Assert.That(input2, Is.EqualTo(inputJson), "Canonicalized input JSON should be deterministic");

            // Проверяем что masked user id / username присутствуют в ожидаемом формате
            // Ensure raw numeric user id property value not present (masked form like U#### is allowed)
            Assert.That(inputJson, Does.Not.Contain("\"UserId\": " + update.Message.From.Id), "Raw user id leaked");
            Assert.That(inputJson, Does.Not.Contain("\"sample_user\""), "Raw username leaked");
            Assert.That(inputJson, Does.Contain("\"Username\": \"u_"), "Masked username missing");
        }
        finally
        {
            // Чистим
            try { Directory.Delete(tmpDir, true); } catch { /* ignore */ }
        }
    }
}
