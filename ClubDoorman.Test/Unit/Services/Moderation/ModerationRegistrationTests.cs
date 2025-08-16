using System.Linq;
using ClubDoorman.Features.Moderation;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services.Moderation;

/// <summary>
/// Тесты, гарантирующие что фасад модерации регистрируется ровно один раз (только через Feature).
/// </summary>
public class ModerationRegistrationTests
{
    [Test]
    public void AddClubDoorman_ShouldRegisterSingleModerationFacade()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddClubDoorman();

        // assert
        var descriptors = services.Where(d => d.ServiceType == typeof(IModerationFacade)).ToList();
        Assert.That(descriptors.Count, Is.EqualTo(1),
            "IModerationFacade должен регистрироваться один раз (в Feature). Удалите дубликаты в legacy модулях.");
    }
}
