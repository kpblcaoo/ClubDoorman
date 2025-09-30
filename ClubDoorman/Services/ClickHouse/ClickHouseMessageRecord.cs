using System;
using System.Diagnostics;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Immutable representation of a row written into ClickHouse.
/// </summary>
[DebuggerDisplay("{ChatId}/{MessageId} @ {EventTs:O}")]
public readonly record struct ClickHouseMessageRecord(
    DateTime EventTs,
    DateTime IngestTs,
    long ChatId,
    string ChatType,
    long MessageId,
    long FromId,
    byte FromIsBot,
    ushort TextLength,
    byte HasUrl,
    byte HasMedia,
    long ReplyToId,
    string IngestSource
);
