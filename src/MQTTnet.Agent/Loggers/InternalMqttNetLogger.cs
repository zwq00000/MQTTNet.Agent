using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace MQTTnet.Agent;

/// <summary>
/// MQTT Client Logger
/// </summary>
internal class InternalMqttNetLogger : IMqttNetLogger {
    private readonly ILogger logger;

    public InternalMqttNetLogger(ILogger<MqttClient> logger) {
        if (logger == null) {
            throw new ArgumentNullException(nameof(logger));
        }
        this.logger = logger;
    }

    internal InternalMqttNetLogger(ILogger logger) {
        if (logger == null) {
            throw new ArgumentNullException(nameof(logger));
        }
        this.logger = logger;
    }

    public bool IsEnabled => this.logger != null;

    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception) {
        switch (logLevel) {
            case MqttNetLogLevel.Warning:
                logger.LogWarning(exception, message, parameters);
                break;
            case MqttNetLogLevel.Error:
                logger.LogError(exception, message, parameters);
                break;
            case MqttNetLogLevel.Info:
                logger.LogInformation(exception, message, parameters);
                break;
            case MqttNetLogLevel.Verbose:
                logger.LogTrace(exception, message, parameters);
                break;
        }
    }
}
