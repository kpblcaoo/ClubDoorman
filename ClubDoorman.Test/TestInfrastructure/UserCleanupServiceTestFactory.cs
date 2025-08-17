using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Moq;
using ClubDoorman.Services.UserManagement;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// Фабрика для создания тестовых экземпляров UserCleanupService
/// </summary>
public class UserCleanupServiceTestFactory
{
    public Mock<ILogger<UserCleanupService>> LoggerMock { get; } = new();
    public Mock<ILogger<ApprovedUsersStorage>> ApprovedUsersStorageLoggerMock { get; } = new();

    /// <summary>
    /// Создает экземпляр UserCleanupService с моками
    /// </summary>
    public UserCleanupService CreateUserCleanupService()
    {
        var approvedUsersStorage = new ApprovedUsersStorage(ApprovedUsersStorageLoggerMock.Object);
        return new UserCleanupService(approvedUsersStorage, LoggerMock.Object);
    }

    /// <summary>
    /// Создает экземпляр UserCleanupService с реальным ApprovedUsersStorage
    /// </summary>
    public UserCleanupService CreateRealUserCleanupService()
    {
        var approvedUsersStorageFactory = new ApprovedUsersStorageTestFactory();
        var approvedUsersStorage = approvedUsersStorageFactory.CreateApprovedUsersStorage();
        return new UserCleanupService(approvedUsersStorage, LoggerMock.Object);
    }
}