using Xunit;
using FluentAssertions;

namespace ClubDoorman.Test.TestKit2.Scenarios;

public class BasicTest
{
    [Fact]
    public void TestKit2_BasicFunctionality_ShouldWork()
    {
        // Arrange
        var message = TestKit2.CreateTestMessage("Hello, world!");
        
        // Act & Assert
        message.Should().NotBeNull();
        message.Text.Should().Be("Hello, world!");
        message.Chat.Should().NotBeNull();
        message.From.Should().NotBeNull();
        
        // Test that TestKit2 is working
        true.Should().BeTrue("TestKit2 is working correctly!");
    }
}
