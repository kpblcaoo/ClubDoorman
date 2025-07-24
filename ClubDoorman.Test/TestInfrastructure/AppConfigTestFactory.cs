using ClubDoorman.Services;
using Moq;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Фабрика для создания тестовых конфигураций IAppConfig
/// </summary>
public static class AppConfigTestFactory
{
    /// <summary>
    /// Создает мок IAppConfig с настройками по умолчанию для тестов
    /// </summary>
    public static Mock<IAppConfig> CreateMock()
    {
        var mock = new Mock<IAppConfig>();
        
        // Настройки по умолчанию для тестов
        mock.Setup(x => x.OpenRouterApi).Returns("test-api-key");
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(true);
        mock.Setup(x => x.MimicryThreshold).Returns(0.7);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(3);
        mock.Setup(x => x.AdminChatId).Returns(123456789);
        mock.Setup(x => x.LogAdminChatId).Returns(123456789);
        mock.Setup(x => x.BotApi).Returns("test-bot-token");
        mock.Setup(x => x.BlacklistAutoBan).Returns(true);
        mock.Setup(x => x.ChannelAutoBan).Returns(true);
        mock.Setup(x => x.LookAlikeAutoBan).Returns(true);
        mock.Setup(x => x.LowConfidenceHamForward).Returns(false);
        mock.Setup(x => x.ApproveButtonEnabled).Returns(true);
        mock.Setup(x => x.ButtonAutoBan).Returns(true);
        mock.Setup(x => x.HighConfidenceAutoBan).Returns(true);
        mock.Setup(x => x.GlobalApprovalMode).Returns(true);
        mock.Setup(x => x.DisableMediaFiltering).Returns(false);
        mock.Setup(x => x.DeleteForwardedMessages).Returns(false);
        mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.WhitelistChats).Returns(new HashSet<long>());
        mock.Setup(x => x.NoVpnAdGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.NoCaptchaGroups).Returns(new HashSet<long>());
        mock.Setup(x => x.AiEnabledChats).Returns(new HashSet<long>());
        mock.Setup(x => x.MediaFilteringDisabledChats).Returns(new HashSet<long>());
        
        // Методы
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(true);
        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsMediaFilteringDisabledForChat(It.IsAny<long>())).Returns(false);
        
        return mock;
    }
    
    /// <summary>
    /// Создает мок IAppConfig с отключенным AI
    /// </summary>
    public static Mock<IAppConfig> CreateMockWithDisabledAi()
    {
        var mock = CreateMock();
        mock.Setup(x => x.OpenRouterApi).Returns((string?)null);
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(false);
        return mock;
    }
    
    /// <summary>
    /// Создает мок IAppConfig с включенным AI
    /// </summary>
    public static Mock<IAppConfig> CreateMockWithEnabledAi()
    {
        var mock = CreateMock();
        mock.Setup(x => x.OpenRouterApi).Returns("real-api-key");
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(true);
        return mock;
    }
    
    /// <summary>
    /// Создает реальную AppConfig для интеграционных тестов
    /// </summary>
    public static IAppConfig CreateReal()
    {
        return new AppConfig();
    }
} 