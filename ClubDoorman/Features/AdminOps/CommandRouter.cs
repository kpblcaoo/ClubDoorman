using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace ClubDoorman.Features.AdminOps;

/// <summary>
/// Маршрутизатор команд, который направляет команды соответствующим обработчикам
/// </summary>
public class CommandRouter : ICommandRouter
{
    private readonly IEnumerable<ICommandHandler> _commandHandlers;
    private readonly ILogger<CommandRouter> _logger;
    private readonly Dictionary<string, ICommandHandler> _handlersByCommand;

    /// <summary>
    /// Создает экземпляр CommandRouter
    /// </summary>
    /// <param name="commandHandlers">Коллекция обработчиков команд</param>
    /// <param name="logger">Логгер</param>
    public CommandRouter(IEnumerable<ICommandHandler> commandHandlers, ILogger<CommandRouter> logger)
    {
        _commandHandlers = commandHandlers ?? throw new ArgumentNullException(nameof(commandHandlers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Создаем словарь для быстрого поиска обработчиков по имени команды
        // Обрабатываем дублирующиеся ключи, используя последний зарегистрированный обработчик
        _handlersByCommand = new Dictionary<string, ICommandHandler>();
        var duplicateCommands = new List<string>();
        
        foreach (var handler in _commandHandlers)
        {
            if (_handlersByCommand.ContainsKey(handler.CommandName))
            {
                duplicateCommands.Add(handler.CommandName);
                _logger.LogWarning("Обнаружен дублирующийся обработчик команды '{Command}': {ExistingHandler} -> {NewHandler}", 
                    handler.CommandName, 
                    _handlersByCommand[handler.CommandName].GetType().Name, 
                    handler.GetType().Name);
            }
            _handlersByCommand[handler.CommandName] = handler;
        }
        
        if (duplicateCommands.Any())
        {
            _logger.LogWarning("Обнаружены дублирующиеся команды: {DuplicateCommands}. Используются последние зарегистрированные обработчики.", 
                string.Join(", ", duplicateCommands.Distinct()));
        }
        
        _logger.LogDebug("CommandRouter создан с {Count} обработчиками команд: {Commands}", 
            _handlersByCommand.Count, string.Join(", ", _handlersByCommand.Keys));
    }

    /// <summary>
    /// Обрабатывает команду, передавая её соответствующему обработчику
    /// </summary>
    /// <param name="message">Сообщение с командой</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>true, если команда была обработана; false, если обработчик не найден</returns>
    public async Task<bool> HandleCommandAsync(Message message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[CommandRouter] Вызван для команды: {Text} (UserId={UserId}, ChatId={ChatId})", message?.Text, message?.From?.Id, message?.Chat?.Id);
        if (message?.Text == null || !message.Text.StartsWith("/"))
        {
            _logger.LogDebug("Сообщение не является командой: {Text}", message?.Text);
            return false;
        }

        var commandText = message.Text.Split(' ')[0].ToLower();
        var command = commandText.StartsWith("/") ? commandText.Substring(1) : commandText;

        _logger.LogDebug("Обработка команды: {Command} от пользователя {UserId} в чате {ChatId}", 
            command, message.From?.Id, message.Chat.Id);

        if (_handlersByCommand.TryGetValue(command, out var handler))
        {
            _logger.LogDebug("Найден обработчик для команды {Command}: {HandlerType}", 
                command, handler.GetType().Name);
            
            await handler.HandleAsync(message, cancellationToken);
            return true;
        }

        _logger.LogDebug("Обработчик для команды {Command} не найден среди зарегистрированных: {RegisteredCommands}", 
            command, string.Join(", ", _handlersByCommand.Keys));
        return false;
    }
}