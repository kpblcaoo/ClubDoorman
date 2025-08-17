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
        return new Update
        {
            Id = 42,
            Type = UpdateType.Message,
            Message = new Message
            {
                MessageId = 7,
                Text = "Hello Test",
                Chat = new Chat { Id = -100123456789, Title = "Test Chat", Type = ChatType.Supergroup },
                From = new User { Id = 555123456, IsBot = false, Username = "sample_user" }
            }
        };
    }

    [Test]
    public void Recorder_WritesInputAndOutput_FilesDeterministic()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "gm_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var recorder = new GoldenMasterRecorder(CreateFlags(tmpDir), new NullLogger<GoldenMasterRecorder>());
            var update = CreateSampleUpdate();
            var correlation = recorder.TryRecordInput(update, "MessageHandler", update.Message!.Chat.Id, update.Message.From!.Id);
            Assert.IsNotNull(correlation, "correlationId null - запись не произошла");
            recorder.TryRecordOutput(correlation, new { kind = "test", value = 123 });

            // Проверяем существование файлов
            var date = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dayDir = Path.Combine(tmpDir, date);
            Assert.IsTrue(Directory.Exists(dayDir), "Day directory not created");
            var files = Directory.GetFiles(dayDir, correlation + "*.json").OrderBy(f => f).ToArray();
            Assert.That(files.Length, Is.EqualTo(2), "Expected input and output files");

            var inputJson = File.ReadAllText(files.First(f => f.EndsWith("input.json")));
            var outputJson = File.ReadAllText(files.First(f => f.EndsWith("output.json")));

            // Повторно вызовем Canonicalize (через второй recorder) и убедимся что содержимое input не меняется при повторной записи с тем же Update
            var recorder2 = new GoldenMasterRecorder(CreateFlags(tmpDir), new NullLogger<GoldenMasterRecorder>());
            var correlation2 = recorder2.TryRecordInput(update, "MessageHandler", update.Message.Chat.Id, update.Message.From.Id);
            Assert.IsNotNull(correlation2);
            var dateDir2 = Path.Combine(tmpDir, date);
            var input2 = File.ReadAllText(Directory.GetFiles(dateDir2, correlation2 + ".input.json").Single());

            Assert.AreEqual(inputJson, input2, "Canonicalized input JSON should be deterministic");

            // Проверяем что masked user id / username присутствуют в ожидаемом формате
            StringAssert.DoesNotContain(update.Message.From.Id.ToString(), inputJson, "Raw user id leaked");
            StringAssert.DoesNotContain("\"sample_user\"", inputJson, "Raw username leaked");
            StringAssert.Contains("\"Username\": \"u_", inputJson, "Masked username missing");
        }
        finally
        {
            // Чистим
            try { Directory.Delete(tmpDir, true); } catch { /* ignore */ }
        }
    }
}
