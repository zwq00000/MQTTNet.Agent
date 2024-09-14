using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization.Metadata;

namespace MQTTnet.Agent;
/// <summary>
/// 消息发布器
/// </summary>
public interface IMessagePublisher {
    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="topic">发布主题</param>
    /// <param name="payload">载荷对象</param>
    /// <param name="typeInfo">JsonMetadata</param>
    /// <param name="retain">消息保留标志,默认为 <see langword="false"/></param>
    /// <param name="qos">quality of service level</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task PublishAsync<T>(string topic, T? payload, JsonTypeInfo<T> typeInfo, bool retain = false, [Range(0, 2)] int qos = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布文本消息
    /// </summary>
    /// <param name="topic">发布主题</param>
    /// <param name="payload">载荷内容</param>
    /// <param name="retain">消息保留标志,默认为 <see langword="false"/></param>
    /// <param name="qos">quality of service level</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task PublishAsync(string topic, string payload, bool retain = false, [Range(0, 3)] int qos = 0, CancellationToken cancellationToken = default(CancellationToken));
}