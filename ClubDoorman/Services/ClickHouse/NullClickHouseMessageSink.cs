using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// No-op sink used when ClickHouse ingestion is disabled.
/// </summary>
public sealed class NullClickHouseMessageSink : IClickHouseMessageSink
{
    public static NullClickHouseMessageSink Instance { get; } = new();

    private NullClickHouseMessageSink()
    {
    }

    public ValueTask<bool> TryEnqueueAsync(ClickHouseMessageRecord record, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }
}
