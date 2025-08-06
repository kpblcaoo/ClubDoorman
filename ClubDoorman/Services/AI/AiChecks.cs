using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Caching;
using System.Net.Http;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services.Core.Configuration;
using ClubDoorman.Services.Telegram;
using ClubDoorman.Services.AI;

namespace ClubDoorman.Services.AI;

public class AiChecks : IAiChecks
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<AiChecks> _logger;
    private readonly IAppConfig _appConfig;
    private readonly OpenAiClient? _api;
    private readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };
    
    // Retry policy для обработки временных ошибок API
    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .AddTimeout(TimeSpan.FromSeconds(30)) // Таймаут 30 секунд на HTTP запросы
        .Build();
    
    const string Model = "google/gemini-2.5-flash";
    
    public AiChecks(ITelegramBotClientWrapper bot, ILogger<AiChecks> logger, IAppConfig appConfig)
    {
        _bot = bot;
        _logger = logger;
        _appConfig = appConfig;
        _api = _appConfig.OpenRouterApi == null ? null : CustomProviders.OpenRouter(_appConfig.OpenRouterApi);
        
        if (_api == null)
        {
            _logger.LogWarning("🤖 AI анализ ОТКЛЮЧЕН: DOORMAN_OPENROUTER_API не настроен или равен 'test-api-key'");
        }
        else
        {
            _logger.LogInformation("🤖 AI анализ ВКЛЮЧЕН: OpenRouter API настроен");
        }
    }

    private static string CacheKey(long userId) => $"ai_profile_check:{userId}";

    /// <summary>
    /// Проверяет, является ли сообщение банальным приветствием
    /// </summary>
    public static bool IsBoringGreeting(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return false;
            
        var text = messageText.Trim().ToLowerInvariant();
        
        var boringGreetings = new[]
        {
            "привет", "hi", "hello", "здравствуйте", "добрый день", "добрый вечер",
            "доброе утро", "hola", "salut", "ciao", "hey", "qq", ".", "йо", "yo",
            "здарова", "приветик", "хай", "хелло", "хеллоу", "дарова", "здоров",
            "здравствуй", "приветствую", "салют", "хело", "hell", "хи", "q",
            "добро пожаловать", "good morning", "good evening", "good day",
            "доброго дня", "доброго вечера", "доброго утра", "всем привет",
            "всех приветствую", "всем хай", "всем здравствуйте"
        };
        
        // Сначала проверяем точное совпадение (включая пунктуацию)
        if (boringGreetings.Contains(text))
            return true;
        
        // Потом убираем знаки препинания и проверяем снова
        var textWithoutPunctuation = text.TrimEnd('.', '!', '?', ',');
        return boringGreetings.Contains(textWithoutPunctuation);
    }

    /// <summary>
    /// Отмечает пользователя как проверенного и безопасного
    /// </summary>
    public void MarkUserOkay(long userId)
    {
        var cacheItem = new CacheItem(CacheKey(userId), new SpamPhotoBio(new SpamProbability(), [], ""));
        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(30) };
        MemoryCache.Default.Set(cacheItem, policy);
        _logger.LogInformation("Пользователь {UserId} отмечен как безопасный", userId);
    }

    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама
    /// </summary>
    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(global::Telegram.Bot.Types.User user, Func<string, Task>? ifChanged = default)
    {
        return await GetAttentionBaitProbability(user, null, ifChanged);
    }
    
    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама (с учетом первого сообщения)
    /// </summary>
    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(global::Telegram.Bot.Types.User user, string? messageText, Func<string, Task>? ifChanged = default)
    {
        if (user == null)
        {
            _logger.LogDebug("Пользователь null, возвращаем пустой результат");
            return new SpamPhotoBio(new SpamProbability(), [], "");
        }

        if (_api == null)
        {
            _logger.LogDebug("OpenAI API не настроен, пропускаем AI проверку профиля");
            return new SpamPhotoBio(new SpamProbability(), [], "");
        }

        var cached = MemoryCache.Default.Get(CacheKey(user.Id)) as SpamPhotoBio;
        if (cached != null)
        {
            _logger.LogDebug("🤖 AI анализ профиля: найден кэш для пользователя {UserId}: вероятность={Probability}, фото={PhotoSize} байт", 
                user.Id, cached.SpamProbability.Probability, cached.Photo.Length);
            return cached;
        }
        
        _logger.LogDebug("🤖 AI анализ профиля: кэш не найден для пользователя {UserId}, выполняем анализ", user.Id);

        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();
        var nameBioUser = string.Empty;

        try
        {
            _logger.LogDebug("🤖 AI анализ профиля: получаем информацию о пользователе {UserId}", user.Id);
            
            // Используем ChatFullInfo для получения Bio и Photo
            var userChat = await _bot.GetChatFullInfo(user.Id);
            _logger.LogDebug("🤖 AI анализ профиля: GetChatFullInfo получен для пользователя {UserId}: Bio={Bio}, Photo={Photo}", 
                user.Id, userChat.Bio ?? "null", userChat.Photo?.ToString() ?? "null");
            
            // Строим профиль без привязанного канала (временно отключено)
            var sb = new StringBuilder();
            sb.Append($"Имя: {Utils.FullName(user)}");
            if (user.Username != null)
                sb.Append($"\nЮзернейм: @{user.Username}");
            if (userChat.Bio != null)
                sb.Append($"\nОписание: {userChat.Bio}");

            nameBioUser = sb.ToString();
            byte[]? photoBytes = null;
            ChatCompletionRequestUserMessage? photoMessage = null;

            // Загружаем фото профиля если есть
            if (userChat.Photo != null)
            {
                _logger.LogDebug("🤖 AI анализ профиля: загружаем фото для пользователя {UserId}, FileId: {FileId}", 
                    user.Id, userChat.Photo.BigFileId);
                    
                try
                {
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms);
                    photoBytes = ms.ToArray();
                    pic = photoBytes;
                    photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
                    sb.Append($"\nФото: прикреплено");
                    
                    _logger.LogDebug("🤖 AI анализ профиля: фото загружено для пользователя {UserId}, размер: {Size} байт", 
                        user.Id, photoBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "🤖 AI анализ профиля: не удалось загрузить фото для пользователя {UserId}", user.Id);
                }
            }
            else
            {
                _logger.LogDebug("🤖 AI анализ профиля: у пользователя {UserId} нет фото профиля", user.Id);
            }

            // Обновляем nameBioUser
            nameBioUser = sb.ToString();

            var prompt = $"""
                Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. 
                Отвечай вероятностью от 0 до 1. 
                
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения), без воды!
                ОБЯЗАТЕЛЬНО анализируй фото профиля если оно есть!
                ОБЯЗАТЕЛЬНО учитывай первое сообщение пользователя если оно предоставлено!
                
                Особенно внимательно учитывай признаки:
                • сексуализированные профили (эмодзи с двойным смыслом - 💦, 💋, 👄, 🍑, 🍆, 🍒, 🍓, 🍌 и прочих в имени, любой намёк на эротику и порно, голые фото)
                • подозрительные фото: слишком привлекательные, профессиональные, эротические
                • упоминания о курсах, заработке, трейдинге, арбитраже, привлечению трафика
                • ссылки на OnlyFans, соцсети
                • род занятий указан прямо в имени (например: HR, SMM, недвижимость, маркетинг)
                • имена-заглушки (случайные буквы, числа, бессмысленные сочетания)
                
                Вот данные профиля:
                {nameBioUser}
                """;

            // Добавляем первое сообщение в промпт, если оно есть
            if (!string.IsNullOrWhiteSpace(messageText))
            {
                prompt += $"\n\nПервое сообщение пользователя:\n{messageText}";
                _logger.LogDebug("🤖 AI анализ профиля: добавлено первое сообщение в анализ для пользователя {UserId}", user.Id);
            }

            // ОТЛАДКА: Логируем полные данные, отправляемые в AI
            _logger.LogDebug("🤖 AI анализ профиля: полные данные для пользователя {UserId}:\n{ProfileData}", user.Id, nameBioUser);
            if (!string.IsNullOrWhiteSpace(messageText))
            {
                _logger.LogDebug("🤖 AI анализ профиля: первое сообщение для пользователя {UserId}: '{MessageText}'", user.Id, messageText);
            }

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — модератор Telegram-группы. Твоя задача — по данным профиля определить, направлен ли аккаунт на само-продвижение или привлечение к сторонним платным/эротическим ресурсам. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения) и по делу.".ToSystemMessage(),
                prompt.ToUserMessage(),
            };
            
            if (photoMessage != null)
                messages.Add(photoMessage);

            _logger.LogDebug("🤖 AI анализ профиля: отправляем запрос в API для пользователя {UserId}, количество сообщений: {MessagesCount}", 
                user.Id, messages.Count);
            _logger.LogDebug("🔍 ОТПРАВКА В AI: messages.Count={Count}, photoMessage={PhotoMessage}", 
                messages.Count, photoMessage != null ? "ЕСТЬ" : "НЕТ");
                
            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("AI анализ профиля пользователя {UserId}: {Probability} - {Reason}", 
                    user.Id, probability.Probability, probability.Reason);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
        }

        var finalResult = new SpamPhotoBio(probability, pic, nameBioUser);
        CacheResult(user.Id, finalResult);
        return finalResult;
    }

    /// <summary>
    /// Анализирует фото профиля на предмет сексуализированного контента
    /// </summary>
    private async ValueTask<SpamPhotoBio> GetEroticPhotoBaitProbability(global::Telegram.Bot.Types.User user, ChatFullInfo userChat)
    {
        if (_api == null)
            return new SpamPhotoBio(new SpamProbability(), [], "");

        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();

        try
        {
            _logger.LogDebug("🤖 AI анализ профиля: GetEroticPhotoBaitProbability для пользователя {UserId}, Photo: {Photo}", 
                user.Id, userChat.Photo?.ToString() ?? "null");
                
            var photo = userChat.Photo!;
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms);
            var photoBytes = ms.ToArray();
            pic = photoBytes;
            
            _logger.LogDebug("🤖 AI анализ профиля: фото загружено в GetEroticPhotoBaitProbability для пользователя {UserId}, размер: {Size} байт", 
                user.Id, photoBytes.Length);
            
            var photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
            var prompt = "Проанализируй, выглядит ли эта аватарка пользователя сексуализированно или развратно. Отвечай вероятностью от 0 до 1.";
            
            var messages = new List<ChatCompletionRequestMessage> 
            { 
                prompt.ToUserMessage(), 
                photoMessage 
            };
            
            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );
            
            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("AI анализ фото профиля пользователя {UserId}: {Probability}", 
                    user.Id, probability.Probability);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при анализе фото профиля пользователя {UserId}", user.Id);
        }

        return new SpamPhotoBio(probability, pic, Utils.FullName(user));
    }

    /// <summary>
    /// Анализирует сообщение на предмет спама с помощью AI
    /// </summary>
    /// <param name="message">Сообщение для анализа</param>
    /// <returns>Вероятность того, что сообщение является спамом</returns>
    /// <exception cref="AiServiceException">Выбрасывается при критических ошибках AI сервиса</exception>
    /// <exception cref="ArgumentNullException">Выбрасывается если message равен null</exception>
    public async ValueTask<SpamProbability> GetSpamProbability(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message), "Сообщение не может быть null");

        if (_api == null)
        {
            _logger.LogDebug("AI API недоступен, возвращаем пустой результат");
            return new SpamProbability();
        }

        try
        {
            var text = message.Text ?? message.Caption ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Текст сообщения пустой, пропускаем AI анализ");
                return new SpamProbability();
            }

            var prompt = $"""
                Проанализируй это сообщение на предмет спама. Отвечай вероятностью от 0 до 1.
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения)!
                
                Признаки спама:
                • Реклама товаров/услуг
                • Просьбы о переходах по ссылкам
                • Навязчивые предложения
                • Массовые рассылки
                • Мошенничество
                
                Сообщение: {text}
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — антиспам модератор. Анализируй сообщения на предмет спама. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения).".ToSystemMessage(),
                prompt.ToUserMessage()
            };

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                _logger.LogDebug("AI анализ сообщения: {Probability} - {Reason}", 
                    response.Value1.Probability, response.Value1.Reason);
                return response.Value1;
            }
            else
            {
                _logger.LogWarning("Получен пустой ответ от AI API для анализа сообщения");
                return new SpamProbability();
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Таймаут при AI анализе сообщения");
            return new SpamProbability();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ошибка сети при AI анализе сообщения");
            return new SpamProbability();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ошибка парсинга JSON при AI анализе сообщения");
            return new SpamProbability();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка при AI анализе сообщения");
            return new SpamProbability();
        }
    }
    
    /// <summary>
    /// Специальный анализ сообщений от подозрительных пользователей с расширенным контекстом
    /// </summary>
    public async ValueTask<SpamProbability> GetSuspiciousUserSpamProbability(
        Message message, 
        global::Telegram.Bot.Types.User user, 
        List<string> firstMessages, 
        double mimicryScore)
    {
        if (_api == null)
            return new SpamProbability();

        try
        {
            var text = message.Text ?? message.Caption ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return new SpamProbability();

            var userName = Utils.FullName(user);
            var username = user.Username != null ? $"@{user.Username}" : "нет";
            var firstMessagesText = string.Join("', '", firstMessages.Take(5));
            
            // Получаем расширенную информацию о пользователе как в основном методе
            var userChat = await _bot.GetChatFullInfo(user.Id);
            var bioInfo = !string.IsNullOrEmpty(userChat.Bio) ? $"\n• Биография: {userChat.Bio}" : "";
            var photoInfo = userChat.Photo != null ? "\n• Есть фото профиля" : "\n• Нет фото профиля";

            var prompt = $"""
                АНАЛИЗ ПОДОЗРИТЕЛЬНОГО ПОЛЬЗОВАТЕЛЯ НА СПАМ
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения)!
                
                КРИТИЧЕСКИЙ КОНТЕКСТ:
                • Пользователь уже помечен как ПОДОЗРИТЕЛЬНЫЙ
                • Его первые сообщения показали признаки мимикрии (скор: {mimicryScore:F2})
                • Это реальный анализ с последствиями (удаление/бан)
                
                ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
                • Имя: {userName}
                • Username: {username}{bioInfo}{photoInfo}
                • Первые сообщения: ['{firstMessagesText}']
                • Скор мимикрии: {mimicryScore:F2} (выше 0.7 = подозрительно)
                
                ТЕКУЩЕЕ СООБЩЕНИЕ: "{text}"
                
                ОСОБОЕ ВНИМАНИЕ К:
                • Предложения займов/денег (особенно после шаблонных приветствий)
                • Финансовые услуги от новых пользователей
                • Переход от невинных сообщений к коммерческим предложениям
                • Типичные схемы мошенников: "Могу дать в долг", "Помогу с деньгами"
                • Несоответствие между первыми сообщениями и текущим
                • Подозрительные фото профиля (слишком привлекательные, эротические)
                • Биография с упоминанием финансов, заработка, услуг
                
                УЧИТЫВАЙ, ЧТО:
                - Высокий скор мимикрии + финансовые предложения = очень вероятный спам
                - Обычные пользователи не предлагают займы незнакомцам
                - Шаблонные приветствия + деньги = классическая схема
                
                Оцени вероятность спама от 0 до 1, учитывая ВСЕ факторы.
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — специализированный антиспам эксперт для анализа подозрительных пользователей. Твоя задача — выявлять мошенников-хамелеонов, которые маскируются под обычных пользователей. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения).".ToSystemMessage(),
                prompt.ToUserMessage()
            };

            // Добавляем фото профиля если есть (как в основном методе)
            if (userChat.Photo != null)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms);
                    var photoBytes = ms.ToArray();
                    var photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
                    messages.Add(photoMessage);
                    _logger.LogDebug("🔍 Добавлено фото профиля для анализа подозрительного пользователя {User}", userName);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Не удалось загрузить фото профиля для {User}: {Error}", userName, ex.Message);
                }
            }

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                _logger.LogDebug("🔍 Специальный AI анализ подозрительного пользователя: {Probability} - {Reason}", 
                    response.Value1.Probability, response.Value1.Reason);
                return response.Value1;
            }

            return new SpamProbability();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при специальном AI анализе подозрительного пользователя");
            return new SpamProbability();
        }
    }



    /// <summary>
    /// Каскадный анализ ML -> AI: комплексная проверка профиля + сообщения + ML результата
    /// </summary>
    public async ValueTask<SpamProbability> GetCascadeAnalysisProbability(
        Message message, 
        global::Telegram.Bot.Types.User user, 
        double mlScore, 
        bool mlSpamDecision)
    {
        if (_api == null)
        {
            _logger.LogDebug("AI API недоступен для каскадного анализа");
            return new SpamProbability();
        }

        try
        {
            var text = message.Text ?? message.Caption ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Текст сообщения пустой, пропускаем каскадный AI анализ");
                return new SpamProbability();
            }

            var userName = Utils.FullName(user);
            var username = user.Username != null ? $"@{user.Username}" : "нет";
            
            // Получаем расширенную информацию о профиле
            var userChat = await _bot.GetChatFullInfo(user.Id);
            var bioInfo = !string.IsNullOrEmpty(userChat.Bio) ? $"\n• Биография: {userChat.Bio}" : "\n• Биография: отсутствует";
            var photoInfo = userChat.Photo != null ? "\n• Есть фото профиля" : "\n• Нет фото профиля";

            // Определяем ML решение текстом
            var mlDecisionText = mlSpamDecision ? "СПАМ" : "НЕ СПАМ";
            var mlConfidenceText = GetMlConfidenceDescription(mlScore);

            var prompt = $"""
                КАСКАДНЫЙ АНАЛИЗ ML → AI
                ВАЖНО: Объяснение должно быть КРАТКИМ (максимум 2-3 предложения)!
                
                КОНТЕКСТ:
                • ML классификатор не уверен в своем решении
                • Требуется экспертная AI оценка для финального решения
                • Это критическая точка модерации - твое решение окончательное
                
                ML РЕЗУЛЬТАТ:
                • Решение ML: {mlDecisionText}
                • Скор ML: {mlScore:F3} ({mlConfidenceText})
                • Статус: НЕУВЕРЕННОСТЬ (требуется AI анализ)
                
                ДАННЫЕ ПОЛЬЗОВАТЕЛЯ:
                • Имя: {userName}
                • Username: {username}{bioInfo}{photoInfo}
                
                СООБЩЕНИЕ ДЛЯ АНАЛИЗА: "{text}"
                
                КРИТЕРИИ ОЦЕНКИ:
                • Анализируй профиль: подозрительные имена, биографии, фото
                • Оценивай текст сообщения: спам-маркеры, коммерческие предложения
                • Учитывай ML скор: если он близок к 0, ML сильно сомневается
                • Обращай внимание на несоответствия между профилем и сообщением
                
                ОСОБЕННОСТИ:
                • Новые пользователи с коммерческими предложениями = подозрительно
                • Привлекательные фото + финансовые услуги = вероятный спам
                • Банальные приветствия могут маскировать спамеров
                • Учитывай региональную специфику и культурные особенности
                
                ЗАДАЧА: Принять окончательное решение вместо ML классификатора.
                Оцени вероятность спама от 0 до 1, учитывая ВСЕ доступные данные.
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — эксперт по антиспам модерации, заменяющий неуверенный ML классификатор. Анализируй КОМПЛЕКСНО: профиль + сообщение + ML данные. Принимай ФИНАЛЬНЫЕ решения. ОБЯЗАТЕЛЬНО отвечай на русском языке! Объяснения должны быть КРАТКИМИ (максимум 2-3 предложения).".ToSystemMessage(),
                prompt.ToUserMessage()
            };

            // Добавляем фото профиля если есть
            ChatCompletionRequestUserMessage? photoMessage = null;
            if (userChat.Photo != null)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms);
                    var photoBytes = ms.ToArray();
                    photoMessage = photoBytes.ToUserMessage(mimeType: "image/jpg");
                    
                    _logger.LogDebug("🤖 Каскадный AI анализ: добавлено фото профиля ({Size} байт) для пользователя {UserId}", 
                        photoBytes.Length, user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось загрузить фото профиля для каскадного анализа пользователя {UserId}", user.Id);
                }
            }
            
            // Добавляем фото к сообщениям если загружено
            if (photoMessage != null)
            {
                messages.Add(photoMessage);
            }

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            _logger.LogInformation("🤖🔗 Каскадный AI анализ завершен: пользователь {UserId}, ML={MlScore}, AI={AiScore}, причина={Reason}", 
                user.Id, mlScore, response.Value1?.Probability ?? 0, response.Value1?.Reason ?? "не указана");
            
            return response.Value1 ?? new SpamProbability();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при каскадном AI анализе пользователя {UserId}", user.Id);
            return new SpamProbability();
        }
    }

    private string GetMlConfidenceDescription(double mlScore)
    {
        return mlScore switch
        {
            > 0.5 => "высокая уверенность в спаме",
            > 0 => "слабая склонность к спаму", 
            > -0.3 => "очень низкая уверенность",
            > -0.6 => "склонность к легитимности",
            _ => "уверенность в легитимности"
        };
    }

    private void CacheResult(long userId, SpamPhotoBio result)
    {
        _logger.LogDebug("🤖 AI анализ профиля: кэшируем результат для пользователя {UserId}: вероятность={Probability}, фото={PhotoSize} байт", 
            userId, result.SpamProbability.Probability, result.Photo.Length);
        var cacheItem = new CacheItem(CacheKey(userId), result);
        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(24) };
        MemoryCache.Default.Set(cacheItem, policy);
    }
}

// Модели данных для AI ответов
public class SpamProbability
{
    public double Probability { get; set; }
    public string Reason { get; set; } = "";
}

public sealed record SpamPhotoBio(SpamProbability SpamProbability, byte[] Photo, string NameBio); 