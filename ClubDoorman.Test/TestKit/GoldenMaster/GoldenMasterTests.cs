using System.Text.Json;
using ClubDoorman.Test.TestKit.Infrastructure;
using ClubDoorman.Test.TestKit.Builders;
using ClubDoorman.Test.TestKit.Fakes;
using NUnit.Framework;

namespace ClubDoorman.Test.TestKit.GoldenMaster;

/// <summary>
/// Golden Master тесты для ключевых сценариев ClubDoorman
/// Фиксируют текущее поведение системы для предотвращения регрессий
/// </summary>
[TestFixture]
[Category("GoldenMaster")]
public class GoldenMasterTests
{
    private const string GoldenMasterDirectory = "GoldenMasterSnapshots";
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Создаем директорию для снапшотов если её нет
        if (!Directory.Exists(GoldenMasterDirectory))
            Directory.CreateDirectory(GoldenMasterDirectory);
    }

    /// <summary>
    /// Тест сценария: участник из банлиста пишет в группу
    /// Ожидается: бан пользователя и уведомления админам
    /// </summary>
    [Test]
    public async Task BanList_UserInGroupChat_ShouldBanAndNotifyAdmins()
    {
        // Arrange
        var testHost = TestHostFactory.CreateForBanListScenario()
            .ConfigureAppConfig(config =>
            {
                config.WithAiEnabled(-1001234567890);
                config.AdminChatId = 123456789;
                config.LogAdminChatId = 987654321;
            });

        var message = EnhancedBuilders.CreateScenarioMessage()
            .FromBannedUser()
            .InGroupChat()
            .WithText("I should be banned for this message")
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert & Record
        await AssertAndRecordGoldenMaster("BanList_UserInGroupChat", result);
    }

    /// <summary>
    /// Тест сценария: участник из банлиста пишет в личку боту
    /// Ожидается: бот не банит, но логирует и уведомляет админов
    /// </summary>
    [Test]
    public async Task BanList_UserInPrivateChat_ShouldLogAndNotifyAdmins()
    {
        // Arrange
        var testHost = TestHostFactory.CreateForBanListScenario();

        var message = EnhancedBuilders.CreateScenarioMessage()
            .FromBannedUser()
            .InPrivateChat()
            .WithText("I'm writing to bot in private")
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert & Record
        await AssertAndRecordGoldenMaster("BanList_UserInPrivateChat", result);
    }

    /// <summary>
    /// Тест сценария: неодобренный первый пользователь пишет сообщение
    /// Ожидается: запуск капчи и блокировка сообщения
    /// </summary>
    [Test]
    public async Task UnapprovedFirstUser_NewMessage_ShouldTriggerCaptcha()
    {
        // Arrange
        var testHost = TestHostFactory.CreateForNewUserScenario()
            .ConfigureAppConfig(config =>
            {
                config.SuspiciousDetectionEnabled = true;
                config.MimicryThreshold = 0.7;
            });

        var message = EnhancedBuilders.CreateScenarioMessage()
            .FromUnapprovedFirstTimeUser()
            .InGroupChat()
            .WithText("Hello everyone, this is my first message!")
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert & Record
        await AssertAndRecordGoldenMaster("UnapprovedFirstUser_NewMessage", result);
    }

    /// <summary>
    /// Тест сценария: работа в тихом режиме
    /// Ожидается: бот не шлёт сообщения в чат, но выполняет модерацию и админ-уведомления
    /// </summary>
    [Test]
    public async Task SilentMode_SpamMessage_ShouldModerateWithoutChatMessages()
    {
        // Arrange
        var testHost = TestHostFactory.CreateForSilentModeScenario()
            .ConfigureAppConfig(config =>
            {
                config.WithSilentMode(true);
                config.AdminChatId = 123456789;
                config.LogAdminChatId = 987654321;
            });

        var message = EnhancedBuilders.CreateScenarioMessage()
            .AsSpam()
            .InGroupChat()
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert & Record
        await AssertAndRecordGoldenMaster("SilentMode_SpamMessage", result);
    }

    /// <summary>
    /// Тест сценария: пересылка с канала
    /// Ожидается: проверка через channel-moderation, при нарушении удаление и логирование
    /// </summary>
    [Test]
    public async Task ChannelForwarding_SuspiciousMessage_ShouldDeleteAndLog()
    {
        // Arrange
        var testHost = TestHostFactory.Create()
            .ConfigureAppConfig(config =>
            {
                config.WithAiEnabled(-1001234567890);
                config.AdminChatId = 123456789;
            });

        var message = EnhancedBuilders.CreateScenarioMessage()
            .ForwardedFromChannel()
            .InGroupChat()
            .WithText("🎰 КАЗИНО! ВЫИГРЫШ ГАРАНТИРОВАН!")
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert & Record
        await AssertAndRecordGoldenMaster("ChannelForwarding_SuspiciousMessage", result);
    }

    /// <summary>
    /// Тест сценария: команда vs обычное сообщение
    /// Ожидается: команда идёт в CommandRouter, обычные сообщения в модератор
    /// </summary>
    [Test]
    public async Task CommandVsMessage_Command_ShouldGoToCommandRouter()
    {
        // Arrange
        var testHost = TestHostFactory.Create();

        var commandMessage = EnhancedBuilders.CreateScenarioMessage()
            .AsCommand("/help")
            .InGroupChat()
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(commandMessage);

        // Assert & Record
        await AssertAndRecordGoldenMaster("CommandVsMessage_Command", result);
    }

    /// <summary>
    /// Тест сценария: обычное сообщение (не команда)
    /// Ожидается: сообщение отправляется в модератор
    /// </summary>
    [Test]
    public async Task CommandVsMessage_RegularMessage_ShouldGoToModerator()
    {
        // Arrange
        var testHost = TestHostFactory.Create();

        var regularMessage = EnhancedBuilders.CreateScenarioMessage()
            .AsRegularText("This is a regular message")
            .InGroupChat()
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(regularMessage);

        // Assert & Record
        await AssertAndRecordGoldenMaster("CommandVsMessage_RegularMessage", result);
    }

    /// <summary>
    /// Тест сценария: новый пользователь присоединяется к чату
    /// Ожидается: запуск процедуры одобрения нового пользователя
    /// </summary>
    [Test]
    public async Task NewUserJoin_FirstTimeJoin_ShouldTriggerApprovalProcess()
    {
        // Arrange
        var testHost = TestHostFactory.CreateForNewUserScenario();

        var newUser = EnhancedBuilders.CreateScenarioUser().AsFirstTimeUser().Build();
        var message = EnhancedBuilders.CreateScenarioMessage()
            .AsNewUserJoined(newUser)
            .InGroupChat()
            .Build();

        // Act
        var result = await testHost.ExecuteMessageScenarioAsync(message);

        // Assert & Record
        await AssertAndRecordGoldenMaster("NewUserJoin_FirstTimeJoin", result);
    }

    /// <summary>
    /// Вспомогательный метод для сохранения и сравнения golden master снапшотов
    /// </summary>
    private async Task AssertAndRecordGoldenMaster(string scenarioName, TestExecutionResult result)
    {
        var snapshotPath = Path.Combine(GoldenMasterDirectory, $"{scenarioName}.json");
        
        // Создаем снапшот текущего результата
        var snapshot = CreateSnapshot(result);
        var currentJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        if (!File.Exists(snapshotPath))
        {
            // Если снапшот не существует, создаем его
            await File.WriteAllTextAsync(snapshotPath, currentJson);
            Assert.Pass($"Golden master snapshot created for {scenarioName}");
            return;
        }

        // Сравниваем с существующим снапшотом
        var expectedJson = await File.ReadAllTextAsync(snapshotPath);
        var expected = JsonSerializer.Deserialize<GoldenMasterSnapshot>(expectedJson);
        
        // Сравниваем ключевые аспекты (игнорируя временные метки)
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Success, Is.EqualTo(expected?.Success), 
                $"Execution success mismatch in {scenarioName}");
            
            Assert.That(snapshot.BotActionsCount, Is.EqualTo(expected?.BotActionsCount), 
                $"Bot actions count mismatch in {scenarioName}");
            
            Assert.That(snapshot.BotActionTypes, Is.EqualTo(expected?.BotActionTypes), 
                $"Bot action types mismatch in {scenarioName}");
            
            Assert.That(snapshot.ErrorMessages, Is.EqualTo(expected?.ErrorMessages), 
                $"Error messages mismatch in {scenarioName}");
        });

        // В случае изменений, обновляем снапшот (для ручного review)
        if (currentJson != expectedJson)
        {
            var updatedPath = snapshotPath.Replace(".json", "_updated.json");
            await File.WriteAllTextAsync(updatedPath, currentJson);
            
            // Это не ошибка, а информация для developer'а
            TestContext.WriteLine($"⚠️ Snapshot updated for {scenarioName}. Review changes in {updatedPath}");
        }
    }

    /// <summary>
    /// Создает снапшот результата выполнения, маскируя нестабильные данные
    /// </summary>
    private static GoldenMasterSnapshot CreateSnapshot(TestExecutionResult result)
    {
        return new GoldenMasterSnapshot
        {
            Success = result.Success,
            BotActionsCount = result.Transcript.BotActions.Count,
            BotActionTypes = result.Transcript.BotActions.Select(a => a.Action).ToList(),
            ErrorMessages = result.Transcript.ErrorMessages,
            LogMessagesCount = result.Transcript.LogMessages.Count,
            AdminNotificationsCount = result.Transcript.AdminNotifications.Count,
            ExecutionTimeMs = (int)result.ExecutionTime.TotalMilliseconds,
            // Маскируем временные метки и ID для стабильности тестов
            BotActionsSummary = result.Transcript.BotActions
                .Select(a => $"{a.Action}({string.Join(",", MaskDynamicValues(a.Parameters))})")
                .ToList()
        };
    }

    /// <summary>
    /// Маскирует динамические значения в параметрах для стабильности снапшотов
    /// </summary>
    private static IEnumerable<string> MaskDynamicValues(object[] parameters)
    {
        return parameters.Select(p => p switch
        {
            DateTime dt => "TIMESTAMP",
            long id when id > 1000000 => "CHAT_ID", 
            int id when id > 1000 => "MESSAGE_ID",
            string s when s.Contains("test") => "TEST_VALUE",
            _ => p?.ToString() ?? "null"
        });
    }
}

/// <summary>
/// Структура снапшота golden master теста
/// </summary>
public class GoldenMasterSnapshot
{
    public bool Success { get; set; }
    public int BotActionsCount { get; set; }
    public List<string> BotActionTypes { get; set; } = new();
    public List<string> BotActionsSummary { get; set; } = new();
    public List<string> ErrorMessages { get; set; } = new();
    public int LogMessagesCount { get; set; }
    public int AdminNotificationsCount { get; set; }
    public int ExecutionTimeMs { get; set; }
}