using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services;
using ClubDoorman.Services.UserBan;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для AiServiceException
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class AiServiceExceptionTestFactory
{

    public AiServiceException CreateAiServiceException()
    {
        return new AiServiceException(
            "Test exception message"
        );
    }

    #region Configuration Methods

    #endregion
}
