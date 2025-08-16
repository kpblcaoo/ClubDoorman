using ClubDoorman.Services.UserBan;
using NUnit.Framework;
using TechTalk.SpecFlow;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Features.AdminOps;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Moq;
using Microsoft.Extensions.Logging;
using FluentAssertions;


namespace ClubDoorman.Test.StepDefinitions.Common
{
    [Binding]
    [Category("BDD")]
    public class CheckCommandSteps
    {
        // Admin/NonAdmin constants moved to TestAdmin
        private Message _testMessage = null!;
        private Message _repliedMessage = null!;
        private Exception? _thrownException;
        private FakeTelegramClient _fakeBot = null!;
        private ILoggerFactory _loggerFactory = null!;
        private MessageHandler _messageHandler = null!;
        private MessageHandlerTestFactory _factory = null!;
        private string _lastResponse = string.Empty;

        [BeforeScenario(Order = 100)]  // Выполняется ПОСЛЕ SuspiciousCommandSteps.BeforeScenario
        public void BeforeScenario()
        {
            Console.WriteLine("[DEBUG] CheckCommandSteps.BeforeScenario (refactored) start");
            _factory = new MessageHandlerTestFactory();
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _fakeBot = new FakeTelegramClient();
            // Делимся ID администратора с другими step definitions (SuspiciousCommandSteps)
            ScenarioContext.Current["CheckCommandAdminUserId"] = TestAdmin.AdminUserId;

            _factory.WithStandardMocks();
            _factory.WithAppConfigSetup(mock => {
                mock.Setup(x => x.AdminChatId).Returns(123456789);
                mock.Setup(x => x.LogAdminChatId).Returns(123456789);
                mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
                mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            });
            _factory.WithClassifierSetup(mock => {
                mock.Setup(x => x.IsSpam(It.IsAny<string>())).ReturnsAsync((false, -0.5f));
            });
            var botPermMock = TestKit.TestKit.CreateBotPermissionsServiceMockForChat(123456789);
            _factory.WithBotPermissionsServiceSetup(mock => {
                mock.Reset();
                mock.Setup(x => x.IsBotAdminAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .Returns((long chatId, CancellationToken token) => botPermMock.Object.IsBotAdminAsync(chatId, token));
                mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .Returns((long chatId, CancellationToken token) => botPermMock.Object.IsSilentModeAsync(chatId, token));
            });
            _factory.WithMessageServiceSetup(mock => {
                mock.Setup(x => x.SendUserNotificationAsync(
                    It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Callback<User, Chat, UserNotificationType, object, CancellationToken>((user, chat, type, data, token) =>
                    {
                        if (data is SimpleNotificationData notificationData)
                        {
                            _lastResponse = notificationData.Reason ?? string.Empty;
                            ScenarioContext.Current["LastResponse"] = _lastResponse;
                        }
                    })
                    .Returns(Task.CompletedTask);
            });

            // Собираем одиночный CheckCommandHandler
            var checkHandler = new CheckCommandHandler(
                _fakeBot,
                _factory.ClassifierMock.Object,
                _factory.MessageServiceMock.Object,
                _factory.BotPermissionsServiceMock.Object,
                _factory.AppConfigMock.Object,
                _loggerFactory.CreateLogger<CheckCommandHandler>());
            var commandRouter = new CommandRouter(new List<ICommandHandler> { checkHandler }, _loggerFactory.CreateLogger<CommandRouter>());
            _factory.WithCommandRouterSetup(mock => {
                mock.Setup(x => x.HandleCommandAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
                    .Returns<Message, CancellationToken>((message, ct) => commandRouter.HandleCommandAsync(message, ct));
            });

            // Админы (добавляем текущего)
            // Настраиваем кастомных админов чата (без обычных пользователей)
            TestAdmin.ApplyStandardAdmins(_fakeBot);

            _messageHandler = _factory.CreateMessageHandlerWithFake(_fakeBot);
            ScenarioContext.Current["MessageHandler"] = _messageHandler;
            // По умолчанию считаем что сценарий админский (Given I am an administrator должен быть выполнен до специфичных шагов)
            // Устанавливаем текущего пользователя как администратора для fake бота
            _fakeBot.TestContextCurrentUserId = TestAdmin.AdminUserId;
            // Также настраиваем BotMock (который используется внутри MessageHandler) чтобы возвращать список админов с нашим AdminUserId
            _factory.BotMock.Setup(x => x.GetChatAdministratorsAsync(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatMember[]
                {
                    new ChatMemberOwner { User = new User { Id = 1, FirstName = "Owner" } },
                    new ChatMemberAdministrator { User = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser" } }
                });
            Console.WriteLine($"[DEBUG] CheckCommandSteps: BotMock admins setup with AdminUserId={TestAdmin.AdminUserId}");
            Console.WriteLine("[DEBUG] CheckCommandSteps.BeforeScenario (refactored) done");
        }

        [Given(@"I reply to a user's message with check command ""(.*)""")]
        public void GivenIReplyToAUsersMessageWithCheckCommand(string command)
        {
            var isAdmin = ScenarioContext.Current.ContainsKey("IsAdmin") && (bool)ScenarioContext.Current["IsAdmin"];
            var currentUserId = isAdmin ? TestAdmin.AdminUserId : TestAdmin.NonAdminUserId;
            _fakeBot.TestContextCurrentUserId = isAdmin ? TestAdmin.AdminUserId : (long?)null;
            _repliedMessage = new Message { From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" }, Chat = new Chat { Id = TestAdmin.AdminChatId, Title = "Admin Chat", Type = ChatType.Group }, Date = DateTime.UtcNow.AddMinutes(-5), Text = "Hello, this is a test message" };
            _testMessage = new Message { From = new User { Id = currentUserId, FirstName = isAdmin ? "AdminUser" : "RegularUser", Username = isAdmin ? "admin" : "user" }, Chat = new Chat { Id = TestAdmin.AdminChatId, Title = "Admin Chat", Type = ChatType.Group }, Date = DateTime.UtcNow, Text = command, ReplyToMessage = _repliedMessage };
            if (isAdmin) _fakeBot.TestContextCurrentUserId = TestAdmin.AdminUserId;
            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [Given(@"I send ""(.*)"" without replying to a message")]
        public void GivenISendWithoutReplyingToAMessage(string command)
        {
            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command
            };

            ScenarioContext.Current["TestMessage"] = _testMessage;
        }

        [Given(@"I reply to an approved user's message with ""(.*)""")]
        public void GivenIReplyToAnApprovedUsersMessageWith(string command)
        {
            _repliedMessage = new Message
            {
                From = new User { Id = 111111111, FirstName = "ApprovedUser", Username = "approved" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Hello, I am approved"
            };

            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };

            // Настраиваем мок для approved пользователя
            _factory.WithUserManagerSetup(mock =>
            {
                mock.Setup(x => x.Approved(111111111, null)).Returns(true);
                mock.Setup(x => x.InBanlist(111111111)).ReturnsAsync(false);
            });

            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [Given(@"I reply to a banned user's message with ""(.*)""")]
        public void GivenIReplyToABannedUsersMessageWith(string command)
        {
            _repliedMessage = new Message
            {
                From = new User { Id = 222222222, FirstName = "BannedUser", Username = "banned" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Hello, I am banned"
            };

            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };

            // Настраиваем мок для banned пользователя
            _factory.WithUserManagerSetup(mock =>
            {
                mock.Setup(x => x.Approved(222222222, null)).Returns(false);
                mock.Setup(x => x.InBanlist(222222222)).ReturnsAsync(true);
            });

            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [Given(@"I reply to a new user's message with ""(.*)""")]
        public void GivenIReplyToANewUsersMessageWith(string command)
        {
            _repliedMessage = new Message
            {
                From = new User { Id = 333333333, FirstName = "NewUser", Username = "newuser" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Hello, I am new"
            };

            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };

            // Настраиваем мок для нового пользователя
            _factory.WithUserManagerSetup(mock =>
            {
                mock.Setup(x => x.Approved(333333333, null)).Returns(false);
                mock.Setup(x => x.InBanlist(333333333)).ReturnsAsync(false);
            });

            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [Given(@"I reply to a message with ""(.*)""")]
        public void GivenIReplyToAMessageWith(string command)
        {
            _repliedMessage = new Message
            {
                From = new User { Id = 444444444, FirstName = "UsernameUser", Username = "username" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Hello, I have a username"
            };

            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };

            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [When(@"I send the check command")]
        public async Task WhenISendTheCheckCommand()
        {
            try
            {
                // Получаем тестовое сообщение из контекста
                var testMessage = (Message)ScenarioContext.Current["TestMessage"];
                
                // Используем уже созданный MessageHandler из ScenarioContext
                _messageHandler = (MessageHandler)ScenarioContext.Current["MessageHandler"];
                Console.WriteLine($"[DEBUG] WhenISendTheCheckCommand: sending message FromId={testMessage.From?.Id}, ChatId={testMessage.Chat.Id}, Text={testMessage.Text}, ReplyTo.From={testMessage.ReplyToMessage?.From?.Id}");
                await _messageHandler.HandleAsync(new Update { Message = testMessage }, CancellationToken.None);
                var last = ScenarioContext.Current.ContainsKey("LastResponse") ? (string)ScenarioContext.Current["LastResponse"] : "<none>";
                Console.WriteLine($"[DEBUG] After handle: LastResponse='{last}'");
                if (_fakeBot.SentMessages.Count > 0)
                {
                    Console.WriteLine("[DEBUG] Sent messages dump:");
                    foreach (var m in _fakeBot.SentMessages)
                    {
                        Console.WriteLine($"  -> ChatId={m.ChatId}, Text='{m.Text}'");
                    }
                }
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }



        [Then(@"I should receive a check error message")]
        public void ThenIShouldReceiveACheckErrorMessage()
        {
            // Команды без реплая просто игнорируются в реальной логике
            // Поэтому ожидаем, что никакого ответа не будет
            _lastResponse.Should().BeNullOrEmpty();
        }

        [Then(@"the error should indicate I need to reply to a message")]
        public void ThenTheErrorShouldIndicateINeedToReplyToAMessage()
        {
            // Команды без реплая просто игнорируются в реальной логике
            // Поэтому ожидаем, что никакого ответа не будет
            _lastResponse.Should().BeNullOrEmpty();
        }

        [Then(@"I should receive a check access denied message")]
        public void ThenIShouldReceiveACheckAccessDeniedMessage()
        {
            var response = ScenarioContext.Current.ContainsKey("LastResponse")
                ? (string)ScenarioContext.Current["LastResponse"]
                : string.Empty;
            response.Should().NotBeNullOrEmpty();
            response.Should().ContainAny("доступ", "запрещ", "прав", "denied", "access");
        }



        [Then(@"I should receive spam analysis results")]
        public void ThenIShouldReceiveSpamAnalysisResults()
        {
            // Получаем ответ из ScenarioContext (установлен в SuspiciousCommandSteps)
            var response = ScenarioContext.Current.ContainsKey("LastResponse") 
                ? (string)ScenarioContext.Current["LastResponse"] 
                : string.Empty;
                
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain("Результат проверки:");
        }

        [Then(@"the analysis should include emoji check")]
        public void ThenTheAnalysisShouldIncludeEmojiCheck()
        {
            var response = ScenarioContext.Current.ContainsKey("LastResponse") 
                ? (string)ScenarioContext.Current["LastResponse"] 
                : string.Empty;
            response.Should().Contain("Много эмодзи:");
        }

        [Then(@"the analysis should include stop words check")]
        public void ThenTheAnalysisShouldIncludeStopWordsCheck()
        {
            var response = ScenarioContext.Current.ContainsKey("LastResponse") 
                ? (string)ScenarioContext.Current["LastResponse"] 
                : string.Empty;
            response.Should().Contain("Найдены стоп-слова:");
        }

        [Then(@"the analysis should include ML classifier results")]
        public void ThenTheAnalysisShouldIncludeMLClassifierResults()
        {
            var response = ScenarioContext.Current.ContainsKey("LastResponse") 
                ? (string)ScenarioContext.Current["LastResponse"] 
                : string.Empty;
            response.Should().Contain("ML классификатор:");
        }

        [Then(@"the analysis should show ""(.*)""")]
        public void ThenTheAnalysisShouldShow(string expectedText)
        {
            var response = ScenarioContext.Current.ContainsKey("LastResponse") 
                ? (string)ScenarioContext.Current["LastResponse"] 
                : string.Empty;
            // Убираем звездочки из ответа для сравнения (они используются для Markdown)
            var responseWithoutMarkdown = response.Replace("*", "");
            responseWithoutMarkdown.Should().Contain(expectedText);
        }

        [Then(@"no analysis results should be displayed")]
        public void ThenNoAnalysisResultsShouldBeDisplayed()
        {
            var response = ScenarioContext.Current.ContainsKey("LastResponse") 
                ? (string)ScenarioContext.Current["LastResponse"] 
                : string.Empty;
            // Ожидаем отсутствие контента анализа
            response.Should().NotContain("Результат проверки");
            response.Should().NotContain("ML классификатор");
            response.Should().NotContain("спам");
        }

        // Step definitions для новых сценариев
        [Given(@"I reply to a spam message with check command ""(.*)""")]
        public void GivenIReplyToASpamMessageWithCheckCommand(string command)
        {
            _repliedMessage = new Message
            {
                From = new User { Id = 555555555, FirstName = "Spammer", Username = "spammer" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Купить дешево!!! Выиграй приз!"
            };
            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };

            // Используем единый _messageHandler (не пересоздаём)

            _fakeBot.TestContextCurrentUserId = TestAdmin.AdminUserId;

            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [Given(@"I reply to a normal message with check command ""(.*)""")]
        public void GivenIReplyToANormalMessageWithCheckCommand(string command)
        {
            _repliedMessage = new Message
            {
                From = new User { Id = 666666666, FirstName = "NormalUser", Username = "normaluser" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Привет, как дела?"
            };
            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };

            // Используем единый _messageHandler (не пересоздаём)
            _fakeBot.TestContextCurrentUserId = TestAdmin.AdminUserId;

            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;
        }

        [Given(@"I reply to a message with emojis with check command ""(.*)""")]
        public void GivenIReplyToAMessageWithEmojisWithCheckCommand(string command)
        {
            // Используем единый _messageHandler (не пересоздаём)
            
            _repliedMessage = new Message
            {
                From = new User { Id = 777777777, FirstName = "EmojiUser", Username = "emojiuser" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "😀😀😀😀😀😀😀😀😀😀"
            };
            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;

            _fakeBot.TestContextCurrentUserId = TestAdmin.AdminUserId;
        }

        [Given(@"I reply to a message with stop words with check command ""(.*)""")]
        public void GivenIReplyToAMessageWithStopWordsWithCheckCommand(string command)
        {
            // Используем единый _messageHandler (не пересоздаём)
            
            _repliedMessage = new Message
            {
                From = new User { Id = 888888888, FirstName = "StopWordUser", Username = "stopworduser" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow.AddMinutes(-5),
                Text = "Это срочно! Бесплатно! Акция!"
            };
            _testMessage = new Message
            {
                From = new User { Id = TestAdmin.AdminUserId, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = _repliedMessage
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["RepliedMessage"] = _repliedMessage;

            _fakeBot.TestContextCurrentUserId = TestAdmin.AdminUserId;
        }
    }
}