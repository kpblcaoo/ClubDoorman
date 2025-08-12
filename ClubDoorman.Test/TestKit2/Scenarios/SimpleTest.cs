using Xunit;
using FluentAssertions;

namespace ClubDoorman.Test.TestKit2.Scenarios;

public class SimpleTest
{
    [Fact]
    public void TestKit2_ShouldCompile()
    {
        // Arrange
        var message = TestKit2.CreateTestMessage("Hello, world!");

        // Act & Assert
        message.Should().NotBeNull();
        message.Text.Should().Be("Hello, world!");
        message.Chat.Should().NotBeNull();
        message.From.Should().NotBeNull();
    }
}
