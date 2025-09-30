using System;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Strongly typed options for ClickHouse ingestion.
/// </summary>
public class ClickHouseOptions
{
    /// <summary>
    /// Enables ClickHouse ingestion when true.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Base HTTP endpoint for ClickHouse (e.g. http://clickhouse:8123).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Target database name; appended as query parameter for writes.
    /// </summary>
    public string Database { get; set; } = "tg";

    /// <summary>
    /// Target table for raw messages (can include database prefix).
    /// </summary>
    public string RawTable { get; set; } = "tg.messages_raw";

    /// <summary>
    /// Constant marker for the data source written alongside each row.
    /// </summary>
    public string IngestSource { get; set; } = "live";

    /// <summary>
    /// Maximum number of records written in a single batch.
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Flush interval used when batch size is not reached (milliseconds).
    /// </summary>
    public int FlushIntervalMilliseconds { get; set; } = 500;

    /// <summary>
    /// Channel capacity for pending rows before dropping starts.
    /// </summary>
    public int ChannelCapacity { get; set; } = 5000;

    /// <summary>
    /// Total retry attempts for a failed ClickHouse write.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay (seconds) between retry attempts; grows linearly per attempt.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// HTTP timeout (seconds) for ClickHouse write requests.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Optional ClickHouse user name for basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional ClickHouse password for basic authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Includes private chat messages when true; defaults to only group chats.
    /// </summary>
    public bool IncludePrivateChats { get; set; }

    /// <summary>
    /// Applies bounds to fields that should always stay positive.
    /// </summary>
    public void Normalize()
    {
        if (BatchSize <= 0) BatchSize = 1;
        if (FlushIntervalMilliseconds < 50) FlushIntervalMilliseconds = 50;
        if (ChannelCapacity < BatchSize) ChannelCapacity = Math.Max(BatchSize * 2, 100);
        if (MaxRetryAttempts < 0) MaxRetryAttempts = 0;
        if (RetryDelaySeconds <= 0) RetryDelaySeconds = 1;
        if (HttpTimeoutSeconds <= 0) HttpTimeoutSeconds = 5;
        if (string.IsNullOrWhiteSpace(IngestSource)) IngestSource = "live";
        if (string.IsNullOrWhiteSpace(Database)) Database = "tg";
        if (string.IsNullOrWhiteSpace(RawTable)) RawTable = "tg.messages_raw";
    }
}
