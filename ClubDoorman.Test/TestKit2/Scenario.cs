using Telegram.Bot.Types;
using ClubDoorman.Services.Handlers;

namespace ClubDoorman.Tests.TestKit2;

public sealed class Scenario
{
    private readonly TestApp _app;
    private Update? _update;

    private Scenario(TestApp app)
    {
        _app = app;
    }

    public static Scenario With(TestApp app) => new(app);

    public Scenario GivenMessage(long chatId, long userId, string text)
    {
        var user = TestBuilders.User().WithId(userId).Build();
        var chat = TestBuilders.Chat().WithId(chatId).Build();
        var message = TestBuilders.Message()
            .WithChat(chat)
            .WithFrom(user)
            .WithText(text)
            .Build();
        
        _update = TestBuilders.Update().WithMessage(message).Build();
        return this;
    }

    public async Task WhenHandled()
    {
        if (_update == null)
            throw new InvalidOperationException("No message given. Call GivenMessage first.");

        var handler = _app.Handler();
        await handler.HandleAsync(_update, default);
    }

    public IReadOnlyList<Effect> ThenEffects()
    {
        var sink = _app.GetService<IEffectsSink>();
        return sink.Snapshot();
    }
}
