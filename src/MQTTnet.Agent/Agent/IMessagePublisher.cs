using System.Text.Json;

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
    /// <param name="retain">消息保留标志,默认为 <see langword="false"/></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task PublishAsync<T>(string topic, T? payload, JsonSerializerOptions? options = null, bool retain = false, CancellationToken cancellationToken = default(CancellationToken)) where T : class;

    /// <summary>
    /// 发布文本消息
    /// </summary>
    /// <param name="topic">发布主题</param>
    /// <param name="payload">载荷内容</param>
    /// <param name="retain">消息保留标志,默认为 <see langword="false"/></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task PublishStringAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default(CancellationToken));
}