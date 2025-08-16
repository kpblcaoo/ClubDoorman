using ClubDoorman.Services.UserJoin;
using ClubDoorman.Features.UserJoin;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Билдер для создания UserJoinFacade с настроенными зависимостями
/// <tags>builders, user-join-facade, fluent-api, test-infrastructure</tags>
/// </summary>
public class UserJoinFacadeBuilder
{
    private readonly Mock<ILogger<UserJoinFacade>> _loggerMock = new();
    private readonly Mock<IUserJoinPolicy> _userJoinPolicyMock = new();

    /// <summary>
    /// Настраивает стандартные зависимости (пока заглушка для совместимости API)
    /// <tags>builders, user-join-facade, standard-mocks</tags>
    /// </summary>
    public UserJoinFacadeBuilder WithStandardMocks() => this;

    /// <summary>
    /// Создает UserJoinFacade с настроенными зависимостями
    /// <tags>builders, user-join-facade, build</tags>
    /// </summary>
    public UserJoinFacade Build() => new UserJoinFacade(_userJoinPolicyMock.Object, _loggerMock.Object);

    /// <summary>
    /// Доступ к мокам политики
    /// </summary>
    public Mock<IUserJoinPolicy> UserJoinPolicyMock => _userJoinPolicyMock;

    /// <summary>
    /// Доступ к мокам логгера
    /// </summary>
    public Mock<ILogger<UserJoinFacade>> LoggerMock => _loggerMock;
}
