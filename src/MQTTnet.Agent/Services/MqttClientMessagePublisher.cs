using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent;

internal class MqttClientMessagePublisher : IMessagePublisher {
    private readonly IMqttClient client;
    private readonly ILogger logger;

    public MqttClientMessagePublisher(IMqttClient client, ILogger<MqttClientMessagePublisher> logger) {
        this.client = client;
        this.logger = logger;
    }

    internal MqttClientMessagePublisher(IMqttClient client, ILogger logger) {
        this.client = client;
        this.logger = logger;
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task CheckConnected(CancellationToken cancellationToken = default) {
        if (this.client.IsConnected) {
            return;
        }

        logger.LogInformation("重新连接 MQTT Broker {server}", client.Options.ChannelOptions.ToString());
        await this.client.ConnectAsync(this.client.Options, cancellationToken);
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="payload">负载</param>
    /// <param name="retain">是否保留消息</param>
    /// <param name="qos">服务质量</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> PublishAsync(string topic, byte[] payload, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        var msg = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
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
    /// 发布消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">主题</param>
    /// <param name="payload">负载</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="retain">是否保留消息</param>
    /// <param name="qos">服务质量</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="JsonException"></exception>
    public async Task<bool> PublishAsync<T>(string topic, T? payload, JsonSerializerOptions options, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }

        if (payload is string payloadStr) {
            return await PublishStringAsync(topic, payloadStr, retain, qos, cancellationToken);
        }

        var bytes = payload == null ? null : JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), options);
        if (bytes == null) {
            return false;
        }
        return await PublishAsync(topic, bytes, retain, qos, cancellationToken);
    }

    /// <summary>
    /// 使用 JsonTypeInfo 发布消息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="topic">主题</param>
    /// <param name="payload">负载</param>
    /// <param name="options">Json 序列化选项</param>
    /// <param name="retain">是否保留消息</param>
    /// <param name="qos">服务质量</param>
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
        if (bytes == null) {
            return false;
        }
        return await PublishAsync(topic, bytes, retain, qos, cancellationToken);
    }

    /// <summary>
    /// 发布字符串消息
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="payload">负载</param>
    /// <param name="retain">是否保留消息</param>
    /// <param name="qos">服务质量</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<bool> PublishStringAsync(string topic, string payload, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(topic)) {
            throw new ArgumentNullException(nameof(topic));
        }
        if (string.IsNullOrWhiteSpace(payload)) {
            throw new ArgumentNullException(nameof(payload));
        }
        var bytes = Encoding.UTF8.GetBytes(payload);
        return await PublishAsync(topic, bytes, retain, qos, cancellationToken);
    }
}