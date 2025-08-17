using ClubDoorman.Effects.AiAnalysis;
using ClubDoorman.Services.AI;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Effects;

[TestFixture]
public class AiAnalysisEffectsTest
{
    private Mock<IAiCascadeService> _aiCascadeServiceMock;
    private Mock<ILogger<RequireAiAnalysisEffect>> _loggerMock;
    private Message _testMessage;
    private User _testUser;

    [SetUp]
    public void Setup()
    {
        _aiCascadeServiceMock = new Mock<IAiCascadeService>();
        _loggerMock = new Mock<ILogger<RequireAiAnalysisEffect>>();
        
        _testUser = new User { Id = 456, Username = "testuser" };
        _testMessage = new Message
        {
            Text = "test message",
            From = _testUser
        };
    }

    [Test]
    public async Task RequireAiAnalysisEffect_ShouldCallAiCascadeService()
    {
        // Arrange
        var mlScore = 0.75;
        var isSilentMode = false;
        var effect = new RequireAiAnalysisEffect(
            _aiCascadeServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            mlScore,
            isSilentMode);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _aiCascadeServiceMock.Verify(
            x => x.HandleAiCascadeAnalysisAsync(_testMessage, _testUser, mlScore, isSilentMode, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task RequireAiAnalysisEffect_WithSilentMode_ShouldPassSilentModeToAiCascadeService()
    {
        // Arrange
        var mlScore = 0.5;
        var isSilentMode = true;
        var effect = new RequireAiAnalysisEffect(
            _aiCascadeServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            mlScore,
            isSilentMode);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _aiCascadeServiceMock.Verify(
            x => x.HandleAiCascadeAnalysisAsync(_testMessage, _testUser, mlScore, isSilentMode, CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task RequireAiAnalysisEffect_WithZeroMlScore_ShouldPassZeroToAiCascadeService()
    {
        // Arrange
        var mlScore = 0.0;
        var isSilentMode = false;
        var effect = new RequireAiAnalysisEffect(
            _aiCascadeServiceMock.Object,
            _loggerMock.Object,
            _testMessage,
            _testUser,
            mlScore,
            isSilentMode);

        // Act
        await effect.ExecuteAsync(CancellationToken.None);

        // Assert
        _aiCascadeServiceMock.Verify(
            x => x.HandleAiCascadeAnalysisAsync(_testMessage, _testUser, mlScore, isSilentMode, CancellationToken.None),
            Times.Once);
    }
}
