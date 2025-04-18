using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent;

internal class MqttClientMessagePublisher : IMessagePublisher {
    private readonly IMqttClient client;
    private readonly JsonSerializerOptions? serializerOptions;
    private readonly ILogger logger;

    public MqttClientMessagePublisher(IMqttClient client, IOptions<JsonOptions>? jsonOptions, ILogger<MqttClientMessagePublisher> logger) {
        this.client = client;
        this.serializerOptions = jsonOptions?.Value.SerializerOptions;
        this.logger = logger;
    }

    internal MqttClientMessagePublisher(IMqttClient client, IOptions<JsonOptions> jsonOptions, ILogger logger) {
        this.client = client;
        this.serializerOptions = jsonOptions.Value.SerializerOptions;
        this.logger = logger;
    }

    private async Task CheckConnected(CancellationToken cancellationToken = default) {
        if (this.client.IsConnected) {
            return;
        }

        logger.LogInformation("重新连接 MQTT Broker {server}", client.Options.ChannelOptions.ToString());
        await this.client.ConnectAsync(this.client.Options, cancellationToken);
    }

    public async Task<bool> PublishAsync<T>(string topic, T? payload, JsonSerializerOptions? options = null, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default) where T : class {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        if (payload is string payloadStr) {
            return await PublishStringAsync(topic, payloadStr, retain, qos, cancellationToken);
        }

        var bytes = payload == null ? null : JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), options ?? serializerOptions);
        var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(bytes)
                    .WithRetainFlag(retain)
                    .WithQualityOfServiceLevel((Protocol.MqttQualityOfServiceLevel)qos)
                    // .WithContentType("application/json")
                    .Build();
        await CheckConnected(cancellationToken);
        var result = await this.client.PublishAsync(msg, cancellationToken);
        if (result.ReasonCode != MqttClientPublishReasonCode.Success) {
            logger.LogWarning("发布主题 {topic} 错误,{code}:{reason}", topic, result.ReasonCode, result.ReasonString);
        }
        return result.IsSuccess;
    }

    /// <summary>
    /// 使用 JsonTypeInfo 发布消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <param name="options"></param>
    /// <param name="retain"></param>
    /// <param name="qos"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<bool> PublishAsync<T>(string topic, T? payload, JsonTypeInfo<T> options, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        if (payload is string payloadStr) {
            return await PublishStringAsync(topic, payloadStr, retain, qos, cancellationToken);
        }

        var bytes = payload == null ? null : JsonSerializer.SerializeToUtf8Bytes(payload, options);
        var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(bytes)
                    .WithRetainFlag(retain)
                    .WithQualityOfServiceLevel((Protocol.MqttQualityOfServiceLevel)qos)
                    // .WithContentType("application/json")
                    .Build();
        await CheckConnected(cancellationToken);
        var result = await this.client.PublishAsync(msg, cancellationToken);
        if (result.ReasonCode != MqttClientPublishReasonCode.Success) {
            logger.LogWarning("发布主题 {topic} 错误,{code}:{reason}", topic, result.ReasonCode, result.ReasonString);
        }
        return result.IsSuccess;
    }

    public async Task<bool> PublishStringAsync(string topic, string payload, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }
        await CheckConnected(cancellationToken);
        var result = await client.PublishStringAsync(topic, payload, retain: retain, qualityOfServiceLevel: (Protocol.MqttQualityOfServiceLevel)qos, cancellationToken: cancellationToken);
        if (result.ReasonCode != MqttClientPublishReasonCode.Success) {
            logger.LogWarning("发布主题 {topic} 错误,{code}:{reason}", topic, result.ReasonCode, result.ReasonString);
        }
        return result.IsSuccess;
    }
}