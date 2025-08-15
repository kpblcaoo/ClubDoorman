using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserFlow;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.TextProcessing;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using ClubDoorman.Models.Requests;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;

namespace ClubDoorman.Features.UserJoin;

/// <summary>
/// Фасад для функциональности присоединения пользователей
/// <tags>user-join, facade, new-members, coordination</tags>
/// </summary>
public class UserJoinFacade : IUserJoinFacade
{
    private readonly IUserJoinPolicy _userJoinPolicy;
    private readonly ILogger<UserJoinFacade> _logger;

    public UserJoinFacade(
        IUserJoinPolicy userJoinPolicy,
        ILogger<UserJoinFacade> logger)
    {
        _userJoinPolicy = userJoinPolicy ?? throw new ArgumentNullException(nameof(userJoinPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Обрабатывает присоединение новых пользователей
    /// <tags>user-join, new-members, processing</tags>
    /// </summary>
    /// <param name="message">Сообщение о новых участниках</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UserJoinFacade: Обрабатываем новых участников");
        await _userJoinPolicy.HandleNewMembersAsync(message, cancellationToken);
    }

    /// <summary>
    /// Обрабатывает одного нового пользователя
    /// <tags>user-join, single-user, processing</tags>
    /// </summary>
    /// <param name="userJoinMessage">Сообщение о присоединении</param>
    /// <param name="user">Пользователь</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UserJoinFacade: Обрабатываем нового пользователя {UserId}", user?.Id);
        await _userJoinPolicy.ProcessNewUserAsync(userJoinMessage, user, cancellationToken);
    }
}
