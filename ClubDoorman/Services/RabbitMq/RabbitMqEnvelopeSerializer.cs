using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Default JSON-based serializer for RabbitMQ envelopes.
/// </summary>
public sealed class RabbitMqEnvelopeSerializer : IRabbitMqEnvelopeSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateParseHandling = DateParseHandling.DateTimeOffset,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    private static readonly JsonSerializer JsonSerializer = JsonSerializer.Create(Settings);

    public byte[] Serialize(RabbitMqUpdateEnvelope envelope)
    {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        if (envelope.Update == null) throw new ArgumentException("Envelope must contain an update", nameof(envelope));

        using var stringWriter = new StringWriter();
        using var jsonWriter = new JsonTextWriter(stringWriter);
        JsonSerializer.Serialize(jsonWriter, envelope);
        jsonWriter.Flush();
        return Encoding.UTF8.GetBytes(stringWriter.ToString());
    }

    public RabbitMqUpdateEnvelope Deserialize(byte[] payload)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        if (payload.Length == 0) throw new ArgumentException("Payload is empty", nameof(payload));
        var json = Encoding.UTF8.GetString(payload);
        using var stringReader = new StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader);
        var result = JsonSerializer.Deserialize<RabbitMqUpdateEnvelope>(jsonReader);
        if (result == null || result.Update == null)
        {
            throw new InvalidOperationException("Payload does not contain a valid update envelope");
        }
        return result;
    }
}
