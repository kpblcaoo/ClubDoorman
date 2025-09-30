using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Asynchronous sink used by the pipeline to enqueue rows for ClickHouse.
/// </summary>
public interface IClickHouseMessageSink
{
    /// <summary>
    /// Attempts to enqueue a ClickHouse row. Returns false when the queue is saturated.
    /// </summary>
    ValueTask<bool> TryEnqueueAsync(ClickHouseMessageRecord record, CancellationToken cancellationToken = default);
}
