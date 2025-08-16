using ClubDoorman.Services.UserBan;
using NUnit.Framework;
using ClubDoorman.Handlers;

using ClubDoorman.Test.TestKit;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types;
using ClubDoorman.Models;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.UserJoin;
using ClubDoorman.Features.UserJoin;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Интеграционные тесты для UserJoinFacade
/// <tags>integration, user-join-facade, new-members, proxy, external-calls</tags>
/// </summary>
[TestFixture]
[Category("integration")]
[Category("user-join-facade")]
public class UserJoinFacadeIntegrationTests
{
    private UserJoinFacadeBuilder _builder = null!;
    private UserJoinFacade _userJoinFacade = null!;
    private Mock<IUserJoinPolicy> _userJoinPolicyMock = null!;
    private Mock<ILogger<UserJoinFacade>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _builder = TK.CreateUserJoinFacadeBuilder()
            .WithStandardMocks();
        _userJoinFacade = _builder.Build();
        _userJoinPolicyMock = _builder.UserJoinPolicyMock;
        _loggerMock = _builder.LoggerMock;
    }

    #region POC Tests (существующие)

    /// <summary>
    /// POC: Проверка создания UserJoinFacade через билдер
    /// <tags>poc, builder, user-join-facade</tags>
    /// </summary>
    [Test]
    public void CreateUserJoinFacade_WithBuilder_ReturnsValidFacade()
    {
        // Arrange & Act
        var userJoinService = TK.CreateUserJoinFacadeBuilder()
            .WithStandardMocks()
            .Build();

        // Assert
        Assert.That(userJoinService, Is.Not.Null);
        Assert.That(userJoinService, Is.InstanceOf<UserJoinFacade>());
    }

    /// <summary>
    /// POC: Проверка доступа к зависимостям через билдер
    /// <tags>poc, builder, dependencies</tags>
    /// </summary>
    [Test]
    public void Builder_ProvidesAccessToDependencies()
    {
        // Arrange
        var builder = TK.CreateUserJoinFacadeBuilder()
            .WithStandardMocks();

        // Act & Assert
        Assert.That(builder.UserJoinPolicyMock, Is.Not.Null);
        Assert.That(builder.LoggerMock, Is.Not.Null);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова HandleNewMembersAsync
    /// <tags>poc, proxy, handle-new-members</tags>
    /// </summary>
    [Test]
    public async Task HandleNewMembersAsync_ValidMessage_ProxiesToPolicy()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateNewUserScenario();

        // Act
        await _userJoinFacade.HandleNewMembersAsync(message, CancellationToken.None);

        // Assert
        _userJoinPolicyMock.Verify(x => x.HandleNewMembersAsync(message, CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// POC: Проверка проксирования вызова ProcessNewUserAsync
    /// <tags>poc, proxy, process-new-user</tags>
    /// </summary>
    [Test]
    public async Task ProcessNewUserAsync_ValidUser_ProxiesToPolicy()
    {
        // Arrange
        var (_, envelope, message, _) = TK.CreateNewUserScenario();
        var user = message.NewChatMembers?.FirstOrDefault() ?? TK.CreateUser();

        // Act
        await _userJoinFacade.ProcessNewUserAsync(message, user, CancellationToken.None);

        // Assert
        _userJoinPolicyMock.Verify(x => x.ProcessNewUserAsync(message, user, CancellationToken.None), Times.Once);
    }

    #endregion
}
