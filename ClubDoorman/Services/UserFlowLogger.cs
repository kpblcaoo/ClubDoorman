using ClubDoorman.Infrastructure;
using Telegram.Bot.Types;
using ClubDoorman.Services.Messaging;

namespace ClubDoorman.Services;

/// <summary>
/// Централизованный логгер для отслеживания пользовательского флоу
/// </summary>
public class UserFlowLogger : IUserFlowLogger
{
    private readonly ILogger<UserFlowLogger> _logger;

    public UserFlowLogger(ILogger<UserFlowLogger> logger)
    {
        _logger = logger;
    }

    public void LogUserJoined(User user, Chat chat, string? joinReason = null)
    {
        var reasonText = !string.IsNullOrEmpty(joinReason) ? $" ({joinReason})" : "";
        _logger.LogInformation("🚪 ПОЛЬЗОВАТЕЛЬ ВОШЕЛ: {User} (id={UserId}) в чат '{ChatTitle}' (id={ChatId}){ReasonText}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogCaptchaShown(User user, Chat chat)
    {
        _logger.LogInformation("🧩 КАПЧА ПОКАЗАНА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogCaptchaPassed(User user, Chat chat)
    {
        _logger.LogInformation("✅ КАПЧА ПРОЙДЕНА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogCaptchaFailed(User user, Chat chat)
    {
        _logger.LogInformation("❌ КАПЧА НЕ ПРОЙДЕНА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogWelcomeShown(User user, Chat chat)
    {
        _logger.LogInformation("👋 ПРИВЕТСТВИЕ ПОКАЗАНО: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogWelcomeRemoved(User user, Chat chat)
    {
        _logger.LogInformation("🗑️ ПРИВЕТСТВИЕ УДАЛЕНО: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogFirstMessage(User user, Chat chat, string messageText)
    {
        var truncatedText = messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText;
        _logger.LogInformation("📝 ПЕРВОЕ СООБЩЕНИЕ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}): {MessageText}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, truncatedText);
    }

    public void LogModerationStarted(User user, Chat chat, string messageText)
    {
        var truncatedText = messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText;
        _logger.LogInformation("🔍 МОДЕРАЦИЯ НАЧАТА: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}): {MessageText}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, truncatedText);
    }

    public void LogSpamListCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        var status = passed ? "✅ ПРОЙДЕНО" : "❌ ЗАБЛОКИРОВАНО";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("📋 СПАМ-СПИСКИ {Status}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogStopWordsCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        var status = passed ? "✅ ПРОЙДЕНО" : "❌ ЗАБЛОКИРОВАНО";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("🚫 СТОП-СЛОВА {Status}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogKnownSpamCheck(User user, Chat chat, bool passed, string? reason = null)
    {
        var status = passed ? "✅ ПРОЙДЕНО" : "❌ ЗАБЛОКИРОВАНО";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("🎯 ИЗВЕСТНЫЙ СПАМ {Status}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogMlAnalysis(User user, Chat chat, bool isSpam, double score, string? reason = null)
    {
        var status = isSpam ? "❌ СПАМ" : "✅ НЕ СПАМ";
        var reasonText = !string.IsNullOrEmpty(reason) ? $" - {reason}" : "";
        _logger.LogInformation("🤖 ML-АНАЛИЗ {Status} (скор {Score:F3}): {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}){ReasonText}", 
            status, score, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reasonText);
    }

    public void LogModerationResult(User user, Chat chat, string action, string reason, double? confidence = null)
    {
        var confidenceText = confidence.HasValue ? $" (уверенность: {confidence.Value:F3})" : "";
        _logger.LogInformation("🎯 РЕЗУЛЬТАТ МОДЕРАЦИИ: {Action} - {Reason}{ConfidenceText} | {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId})", 
            action, reason, confidenceText, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id);
    }

    public void LogUserApproved(User user, Chat chat, string reason)
    {
        _logger.LogInformation("✅ ПОЛЬЗОВАТЕЛЬ ОДОБРЕН: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserBanned(User user, Chat chat, string reason)
    {
        _logger.LogInformation("🚫 ПОЛЬЗОВАТЕЛЬ ЗАБАНЕН: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserRestricted(User user, Chat chat, string reason, TimeSpan? duration = null)
    {
        var durationText = duration.HasValue ? $" на {duration.Value.TotalMinutes:F0} минут" : "";
        _logger.LogInformation("⚠️ ПОЛЬЗОВАТЕЛЬ ОГРАНИЧЕН{durationText}: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            durationText, Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserRemovedFromApproved(User user, Chat chat, string reason)
    {
        _logger.LogInformation("🗑️ ПОЛЬЗОВАТЕЛЬ УДАЛЕН ИЗ ОДОБРЕННЫХ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserAddedToApproved(User user, Chat chat, string reason)
    {
        _logger.LogInformation("✅ ПОЛЬЗОВАТЕЛЬ ДОБАВЛЕН В ОДОБРЕННЫЕ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogUserMarkedAsSuspicious(User user, Chat chat, double mimicryScore, List<string> firstMessages)
    {
        var messagesText = string.Join(", ", firstMessages.Select(m => $"\"{m}\""));
        _logger.LogInformation("🎭 ПОЛЬЗОВАТЕЛЬ ПОМЕЧЕН КАК ПОДОЗРИТЕЛЬНЫЙ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - скор мимикрии: {Score:F2}, первые сообщения: [{Messages}]", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, mimicryScore, messagesText);
    }

    public void LogUserRemovedFromSuspicious(User user, Chat chat, string reason)
    {
        _logger.LogInformation("🔓 ПОЛЬЗОВАТЕЛЬ УДАЛЕН ИЗ ПОДОЗРИТЕЛЬНЫХ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, reason);
    }

    public void LogAiProfileAnalysis(User user, Chat chat, double spamProbability, string reason)
    {
        _logger.LogInformation("🤖 AI АНАЛИЗ ПРОФИЛЯ: {User} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) - вероятность спама: {Probability:F2}, причина: {Reason}", 
            Utils.FullName(user), user.Id, chat.Title ?? "неизвестно", chat.Id, spamProbability, reason);
    }

    public void LogChannelMessage(Chat senderChat, Chat targetChat, string messageText)
    {
        var truncatedText = messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText;
        _logger.LogInformation("📢 СООБЩЕНИЕ ОТ КАНАЛА: канал '{SenderChat}' (id={SenderChatId}) в чате '{TargetChat}' (id={TargetChatId}): {MessageText}", 
            senderChat.Title ?? "неизвестно", senderChat.Id, targetChat.Title ?? "неизвестно", targetChat.Id, truncatedText);
    }

    public void LogSystemError(Exception exception, string context, User? user = null, Chat? chat = null)
    {
        var userText = user != null ? $" пользователя {Utils.FullName(user)} (id={user.Id})" : "";
        var chatText = chat != null ? $" в чате '{chat.Title}' (id={chat.Id})" : "";
        _logger.LogError(exception, "🚨 СИСТЕМНАЯ ОШИБКА{UserText}{ChatText} - {Context}", userText, chatText, context);
    }
} 