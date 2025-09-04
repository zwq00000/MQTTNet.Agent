namespace MQTTnet.Agent;
public record MqttConnectionOptions {

    /// <summary>
    /// MQTT Broker connection url
    /// </summary>
    /// <value></value>
    public Uri ConnectionUri { get; set; }

    /// <summary>
    /// MQTT Broker Login User
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// MQTT Broker Login Password
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// MQTT Broker Login Password
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// MQTT Client Clean Session
    /// </summary>

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
        if (!string.IsNullOrEmpty(ClientId)) {
            builder.WithClientId(ClientId);
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
        if (!string.IsNullOrEmpty(ClientId)) {
            builder.WithClientId(ClientId);
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
        if (!string.IsNullOrEmpty(ClientId)) {
            builder.WithClientId(ClientId);
        }
        return builder.Build();
    }
}
