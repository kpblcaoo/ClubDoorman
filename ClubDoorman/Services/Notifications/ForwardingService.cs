                        await DeleteAndReportToLogChat(message, moderationResult.Reason, cancellationToken);
                    }
                    else
                    {
                        await DeleteAndReportMessage(message, moderationResult.Reason, isSilentMode, cancellationToken);
                    }
                    _logger.LogInformation("Сообщение успешно обработано для удаления");
                    
                    // Отслеживаем нарушения для повторных банов
                    await _userBanService.TrackViolationAndBanIfNeededAsync(message, user, moderationResult.Reason, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при удалении сообщения: {Reason}", moderationResult.Reason);
                }
                break;
            
            case ModerationAction.Report:
                _logger.LogInformation("Отправка в админ-чат: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
                break;
            
            case ModerationAction.RequireManualReview:
                _logger.LogInformation("Требует ручной проверки: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, isSilentMode, cancellationToken);
                break;
            
            case ModerationAction.RequireAiAnalysis:
                _logger.LogInformation("ML не уверен, запускаем AI анализ: {Reason}", moderationResult.Reason);
                await HandleAiCascadeAnalysis(message, user, moderationResult.Confidence ?? 0, isSilentMode, cancellationToken);
                break;
        }
    }

    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        try
        {
            if (chat.Type != ChatType.Supergroup)
                return false;

            // Проверяем, является ли это автоматическим пересыланием из канала
            var isAutoForward = message.IsAutomaticForward;
            
            if (isAutoForward)
            {
                _logger.LogDebug("Обнаружено обсуждение канала: chat={ChatId}, autoForward={AutoForward}", 
                    chat.Id, message.IsAutomaticForward);
            }
            
            return isAutoForward;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось определить тип чата {ChatId}", chat.Id);
            return false;
        }
    }





    public async Task DeleteAndReportToLogChat(Message message, string reason, CancellationToken cancellationToken)
