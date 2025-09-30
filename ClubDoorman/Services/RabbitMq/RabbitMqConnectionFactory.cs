using System;
using ClubDoorman.Services.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ClubDoorman.Services.RabbitMq;

/// <summary>
/// Thin wrapper around the official RabbitMQ client factory with opinionated defaults.
/// </summary>
public sealed class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;

    public RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnectionFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_options.Uri))
        {
            throw new InvalidOperationException("RabbitMQ URI is not configured.");
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(_options.Uri),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            DispatchConsumersAsync = true,
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        };

        try
        {
            var connection = factory.CreateConnection();
            _logger.LogInformation("RabbitMQ connection established to {Host}", connection.Endpoint.HostName);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection to {Uri}", _options.Uri);
            throw;
        }
    }
}
