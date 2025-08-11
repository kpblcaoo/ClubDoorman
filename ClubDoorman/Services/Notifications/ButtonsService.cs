    public async Task SendSuspiciousMessageWithButtons(Message message, User user, SuspiciousMessageNotificationData data, bool isSilentMode, CancellationToken cancellationToken)
    {
        try
        {
            var template = _messageService.GetTemplates().GetAdminTemplate(AdminNotificationType.SuspiciousMessage);
            var messageText = _messageService.GetTemplates().FormatNotificationTemplate(template, data);
            
            // Добавляем префикс тихого режима если нужно
            if (isSilentMode)
            {
                messageText = $"🔇 **Тихий режим**\n\n{messageText}";
            }
            
            // Создаем кнопки реакции для админ-чата (стандартные кнопки: бан, пропуск, свой)
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                    new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                    new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
                }
            });
            
            // ФИКС: Пытаемся переслать сообщение, но если не получается - отправляем без пересылки
            Message? forward = null;
            try
            {
                // Сначала пересылаем оригинальное сообщение
                forward = await _bot.ForwardMessage(
                    new ChatId(_appConfig.AdminChatId),
                    message.Chat.Id,
                    message.MessageId,
                    cancellationToken: cancellationToken
                );
                
                // Отправляем уведомление с кнопками как ответ на форвард
                await _bot.SendMessage(
                    _appConfig.AdminChatId,
                    messageText,
                    parseMode: ParseMode.Html,
                    replyParameters: forward,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception forwardEx) when (forwardEx.Message.Contains("protected content") || forwardEx.Message.Contains("can't be forwarded"))
            {
                _logger.LogWarning("Сообщение имеет защищенный контент, отправляем уведомление без пересылки: {Error}", forwardEx.Message);
                
                // Отправляем уведомление без пересылки (просто как обычное сообщение)
                await _bot.SendMessage(
                    _appConfig.AdminChatId,
                    messageText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
            
            _logger.LogDebug("Отправлено подозрительное сообщение с кнопками для пользователя {User} в чате {Chat}", 
                Utils.FullName(user), message.Chat.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке подозрительного сообщения с кнопками");
            // Fallback: отправляем без кнопок
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.SuspiciousMessage, data, cancellationToken);
        }
    }

    /// <summary>
    /// Обработка бана пользователя по блэклисту lols.bot при первом сообщении
    /// </summary>


    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    internal void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }


