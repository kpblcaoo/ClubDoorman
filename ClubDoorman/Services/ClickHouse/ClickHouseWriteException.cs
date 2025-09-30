using System;
using System.Net;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Exception thrown when ClickHouse rejects an insert request.
/// </summary>
public sealed class ClickHouseWriteException : Exception
{
    public ClickHouseWriteException(HttpStatusCode statusCode, string responseBody)
        : base($"ClickHouse write failed with status {(int)statusCode} ({statusCode}). Response: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }
}
