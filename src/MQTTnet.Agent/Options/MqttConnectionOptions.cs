using Microsoft.Extensions.Options;
using MQTTnet.Client;

namespace MQTTnet.Agent;
public class MqttConnectionOptions : IOptions<MqttConnectionOptions> {

    /// <summary>
    /// MQTT Broker connection url
    /// </summary>
    /// <value></value>
    public Uri ConnectionUri { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public bool ClearSession { get; set; } = true;


     public MqttClientOptions BuildClientOptions() {
        if (ConnectionUri == null) {
            throw new ArgumentNullException(nameof(ConnectionUri));
        }
        var builder = new MqttClientOptionsBuilder().WithConnectionUri(ConnectionUri);

        if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password)) {
            builder.WithCredentials(UserName, Password);
        }
        if (ClearSession) {
            builder.WithCleanSession();
        }
        return builder.Build();
    }

    public MqttClientOptions Build(MqttClientOptionsBuilder builder) {
        if (ConnectionUri == null) {
            throw new ArgumentNullException(nameof(ConnectionUri));
        }
        builder.WithConnectionUri(ConnectionUri);
        if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password)) {
            builder.WithCredentials(UserName, Password);
        }
        if (ClearSession) {
            builder.WithCleanSession();
        }
        return builder.Build();
    }

    public MqttClientOptions Build(string userName, string password) {
        var builder = new MqttClientOptionsBuilder();
        builder.WithConnectionUri(ConnectionUri)
        .WithCredentials(userName, password);
        if (ClearSession) {
            builder.WithCleanSession();
        }
        return builder.Build();
    }
    MqttConnectionOptions IOptions<MqttConnectionOptions>.Value => this;
}
