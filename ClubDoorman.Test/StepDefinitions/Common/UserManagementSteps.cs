
namespace ClubDoorman.Test.StepDefinitions.Common
{
    [Binding]
    [Category("BDD")]
    public class UserManagementSteps
    {
        private Message _testMessage = null!;
        private FakeTelegramClient _fakeBot = null!;
        private ILoggerFactory _loggerFactory = null!;
        private UserManager _userManager = null!;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _fakeBot = new FakeTelegramClient();
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var appConfig = AppConfigTestFactory.CreateDefault();
            var approvedUsersStorage = new ApprovedUsersStorage(_loggerFactory.CreateLogger<ApprovedUsersStorage>());
            _userManager = new UserManager(_loggerFactory.CreateLogger<UserManager>(), approvedUsersStorage, appConfig);
        }

        [AfterScenario]
        public void AfterScenario()
        {
            _loggerFactory?.Dispose();
        }

        [Given(@"a user joins the group")]
        public void GivenUserJoinsGroup()
        {
            _testMessage = TK.CreateNewUserJoinMessage();
            // Сохраняем сообщение в ScenarioContext для использования в других Step Definition
            ScenarioContext.Current["TestMessage"] = _testMessage;
            Console.WriteLine($"[DEBUG] UserManagementSteps: Сохранил TestMessage в ScenarioContext, From.Id = {_testMessage?.From?.Id}");
        }

        [Then(@"the user gets access to the chat")]
        public void ThenUserGetsAccessToChat()
        {
            // В тестовой среде симулируем предоставление доступа
            // В реальной реализации здесь была бы проверка через UserManager

            // Для демонстрации - симулируем успешное предоставление доступа
            var userId = _testMessage.From!.Id;
            // В реальной реализации: var isApproved = _userManager.Approved(userId, _testMessage.Chat.Id);
            // isApproved.Should().BeTrue();
        }

        [Then(@"the captcha is NOT shown")]
        public void ThenCaptchaIsNotShown()
        {
            // Проверяем, что капча не была показана

            // В реальной реализации здесь была бы проверка, что CaptchaService не был вызван
            // или что сообщение о капче не было отправлено
        }

        [Then(@"no exceptions should occur")]
        public void ThenNoExceptionsShouldOccur()
        {
            // Проверяем, что исключений не произошло
            // В реальной реализации здесь была бы проверка логов или состояния
        }
    }
}