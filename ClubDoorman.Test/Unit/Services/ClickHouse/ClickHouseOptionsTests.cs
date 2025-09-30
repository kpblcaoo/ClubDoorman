using ClubDoorman.Services.ClickHouse;
using NUnit.Framework;

namespace ClubDoorman.Test.Unit.Services.ClickHouse;

[TestFixture]
public class ClickHouseOptionsTests
{
    [Test]
    public void Normalize_ClampsValuesAndDefaults()
    {
        var options = new ClickHouseOptions
        {
            BatchSize = -1,
            FlushIntervalMilliseconds = 0,
            ChannelCapacity = 10,
            MaxRetryAttempts = -5,
            RetryDelaySeconds = 0,
            HttpTimeoutSeconds = 0,
            IngestSource = "",
            Database = "",
            RawTable = ""
        };

        options.Normalize();

        Assert.That(options.BatchSize, Is.GreaterThan(0));
        Assert.That(options.FlushIntervalMilliseconds, Is.GreaterThanOrEqualTo(50));
        Assert.That(options.ChannelCapacity, Is.GreaterThanOrEqualTo(options.BatchSize));
        Assert.That(options.MaxRetryAttempts, Is.GreaterThanOrEqualTo(0));
        Assert.That(options.RetryDelaySeconds, Is.GreaterThanOrEqualTo(1));
        Assert.That(options.HttpTimeoutSeconds, Is.GreaterThanOrEqualTo(5));
        Assert.That(options.IngestSource, Is.EqualTo("live"));
        Assert.That(options.Database, Is.EqualTo("tg"));
        Assert.That(options.RawTable, Is.EqualTo("tg.messages_raw"));
    }
}
