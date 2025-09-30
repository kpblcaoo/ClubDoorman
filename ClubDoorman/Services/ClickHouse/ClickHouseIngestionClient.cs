using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClubDoorman.Services.ClickHouse;

/// <summary>
/// Performs HTTP inserts into ClickHouse using the JSONEachRow format.
/// </summary>
public sealed class ClickHouseIngestionClient : IClickHouseIngestionClient, IDisposable
{
    private readonly ClickHouseOptions _options;
    private readonly ILogger<ClickHouseIngestionClient> _logger;
    private readonly HttpClient? _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    public ClickHouseIngestionClient(IOptions<ClickHouseOptions> options, ILogger<ClickHouseIngestionClient> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _options.Normalize();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (_options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(_options.Url))
            {
                throw new InvalidOperationException("ClickHouse URL must be configured when ingestion is enabled.");
            }

            var trimmed = _options.Url.Trim();
            if (!trimmed.EndsWith('/'))
            {
                trimmed += "/";
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(trimmed, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds)
            };

            if (!string.IsNullOrEmpty(_options.Username))
            {
                var credential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password ?? string.Empty}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credential);
            }
        }
    }

    public async Task InsertAsync(IReadOnlyList<ClickHouseMessageRecord> batch, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || batch.Count == 0)
        {
            return;
        }

        if (_httpClient == null)
        {
            throw new InvalidOperationException("ClickHouse HTTP client is not initialized.");
        }

        var builder = new StringBuilder(batch.Count * 128);
        builder.Append("INSERT INTO ").Append(_options.RawTable).Append(" FORMAT JSONEachRow\n");

        foreach (var record in batch)
        {
            var payload = new ClickHouseRowPayload(record);
            var json = JsonSerializer.Serialize(payload, _serializerOptions);
            builder.Append(json).Append('\n');
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"?database={Uri.EscapeDataString(_options.Database)}")
        {
            Content = new StringContent(builder.ToString(), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("ClickHouse write failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new ClickHouseWriteException(response.StatusCode, body);
        }
    }

    public async Task PingAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (_httpClient == null)
        {
            throw new InvalidOperationException("ClickHouse HTTP client is not initialized.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "?query=SELECT%201");
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new ClickHouseWriteException(response.StatusCode, body);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    private readonly struct ClickHouseRowPayload
    {
        [JsonPropertyName("event_ts")]
        public DateTime EventTs { get; }

        [JsonPropertyName("ingest_ts")]
        public DateTime IngestTs { get; }

        [JsonPropertyName("chat_id")]
        public long ChatId { get; }

        [JsonPropertyName("chat_type")]
        public string ChatType { get; }

        [JsonPropertyName("msg_id")]
        public long MessageId { get; }

        [JsonPropertyName("from_id")]
        public long FromId { get; }

        [JsonPropertyName("from_is_bot")]
        public int FromIsBot { get; }

        [JsonPropertyName("text_len")]
        public int TextLength { get; }

        [JsonPropertyName("has_url")]
        public int HasUrl { get; }

        [JsonPropertyName("has_media")]
        public int HasMedia { get; }

        [JsonPropertyName("reply_to_id")]
        public long ReplyToId { get; }

        [JsonPropertyName("ingest_source")]
        public string IngestSource { get; }

        public ClickHouseRowPayload(ClickHouseMessageRecord record)
        {
            EventTs = record.EventTs;
            IngestTs = record.IngestTs;
            ChatId = record.ChatId;
            ChatType = record.ChatType;
            MessageId = record.MessageId;
            FromId = record.FromId;
            FromIsBot = record.FromIsBot;
            TextLength = record.TextLength;
            HasUrl = record.HasUrl;
            HasMedia = record.HasMedia;
            ReplyToId = record.ReplyToId;
            IngestSource = record.IngestSource;
        }
    }
}
