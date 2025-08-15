namespace MQTTnet.Agent;
/// <summary>
/// 订阅/发布 消息代理接口
/// 支持基于主题的消息异步订阅发布
/// </summary>
public interface IMessageAgent : IMessageReader, IMessagePublisher, IDisposable {

}
