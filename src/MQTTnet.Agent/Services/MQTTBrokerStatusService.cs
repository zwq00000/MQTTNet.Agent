
using Microsoft.Extensions.Logging;
namespace MQTTnet.Agent;

/// <summary>
/// MQTT Broker 状态服务
/// </summary>
internal class MQTTBrokerStatusService : IDisposable {
    private readonly IMessageSubscriber subscriber;
    private ILogger<MQTTBrokerStatusService> logger;

    public MQTTBrokerStatusService(IMessageSubscriber subscriber, ILogger<MQTTBrokerStatusService> logger) {
        this.subscriber = subscriber;
        this.logger = logger;
        subscriber.SubscribeAsync<string>("$SYS/#").ContinueWith(t => {
            if (t.IsCompleted) {
                t.Result.Subscribe(OnReceived);
            }
        });
    }

    public void Dispose() {
        this.subscriber.Dispose();
    }

    private void OnReceived(MessageArgs<string> msg) {
        logger.LogInformation("recive {msg}", msg);
    }
}
