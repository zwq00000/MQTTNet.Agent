using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace MQTTnet.Agent;

internal class MqttClientMessagePublisher : IMessagePublisher {
    private readonly IMqttClient client;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly ILogger logger;

    public MqttClientMessagePublisher(IMqttClient client, IOptions<JsonOptions> jsonOptions, ILogger<MqttClientMessagePublisher> logger) {
        this.client = client;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
        this.logger = logger;
    }

    internal MqttClientMessagePublisher(IMqttClient client, IOptions<JsonOptions> jsonOptions, ILogger logger) {
        this.client = client;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
        this.logger = logger;
    }

    private async Task CheckConnected(CancellationToken cancellationToken = default){
        if(this.client.IsConnected){
            return ;
        }

        logger.LogInformation("重新连接 MQTT Broker {server}",client.Options.ChannelOptions.ToString());
        await this.client.ConnectAsync(this.client.Options,cancellationToken);
    }

    public async Task PublishAsync<T>(string topic, T? payload, JsonSerializerOptions? options = null, bool retain = false, CancellationToken cancellationToken = default) where T : class {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        var bytes = payload == null ? null : JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), options??serializerOptions);
        var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(bytes)
                    .WithRetainFlag(retain)
                    .WithContentType("application/json")
                    .Build();
        await CheckConnected(cancellationToken);
        var result = await this.client.PublishAsync(msg, cancellationToken);
        if (result.ReasonCode != MqttClientPublishReasonCode.Success) {
            logger.LogWarning("发布主题 {topic} 错误,{code}:{reason}", topic, result.ReasonCode, result.ReasonString);
        }
    }

    public async Task PublishStringAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }
        await CheckConnected(cancellationToken);
        await client.PublishStringAsync(topic, payload, retain: retain, cancellationToken: cancellationToken);
    }
}