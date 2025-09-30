using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Low-level client that performs HTTP writes to ClickHouse.
/// </summary>
public interface IClickHouseIngestionClient
{
    /// <summary>
    /// Writes a batch of rows into ClickHouse.
    /// </summary>
    Task InsertAsync(IReadOnlyList<ClickHouseMessageRecord> batch, CancellationToken cancellationToken);

    /// <summary>
    /// Performs a lightweight availability check.
    /// </summary>
    Task PingAsync(CancellationToken cancellationToken);
}
