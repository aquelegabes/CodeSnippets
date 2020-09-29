#region
// Requires package:
// <PackageReference Include="MQTTnet"/>
#endregion

#region
// This handler assumes usage of rabbit as a mqtt broker.
// So receive messages using RabbitHandler
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;

namespace IoTDeviceManager.Core.RabbitMQ
{
    /// <summary>
    /// Model responsible for holding a mqtt connection info.
    /// </summary>
    public sealed class MQTTConnection
    {
        public string Host { get; set; }
        /// <summary>
        /// Port to connect to the <see cref="Host"/>.
        /// </summary>
        /// <value>Default MQTT port is 1883.</value>
        public int Port { get; set; } = 1883;
        public string User { get; set; }
        public string Password { get; set; }
        public bool IsCredentialsAvailable => !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Password);
    }

    /// <summary>
    /// Handler responsible for publishing MQTT payloads.
    /// </summary>
    public sealed class MQTTHandler : IDisposable
    {
        private IMqttClient Client { get; }
        private IMqttClientOptions Options { get; }

        /// <summary>
        /// Constructor responsible for obtaining a <see cref="MQTTConnection"/> through DI.
        /// </summary>
        /// <param name="connectionInfo">Valid connection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="connectionInfo"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><see cref="MQTTConnection.Host"/> is null or empty.</exception>
        public MQTTHandler(MQTTConnection connectionInfo)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException(paramName: nameof(connectionInfo), message: "Connection info must not be null.");
            if (string.IsNullOrWhiteSpace(connectionInfo.Host))
                throw new ArgumentNullException(paramName: nameof(connectionInfo.Host), message: "Host must not be null.");

            var factory = new MqttFactory();
            Client = factory.CreateMqttClient();
            var optsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(opts =>
                    {
                        opts.Server = connectionInfo.Host;
                        opts.Port = connectionInfo.Port;
                        opts.TlsOptions = new MqttClientTlsOptions { UseTls = false };
                    });

            if (connectionInfo.IsCredentialsAvailable)
                optsBuilder.WithCredentials(connectionInfo.User, connectionInfo.Password);

            Options = optsBuilder.Build();
        }

        /// <summary>
        /// Send a message to the MQTT broker asynchronously.
        /// </summary>
        /// <param name="topic">Topic that the message will be sent to.</param>
        /// <param name="payload">Message payload.</param>
        /// <param name="qualityOfService">
        /// <para>Optional: packet quality of service.</para>
        /// <para>Default value is <see cref="MqttQualityOfServiceLevel.AtLeastOnce"/>.</para>
        /// </param>
        /// <param name="cancellationToken">Optional: cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="topic"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="payload"/> is null.</exception>
        public async Task SendMessage(
            string topic, byte[] payload, MqttQualityOfServiceLevel qualityOfService = MqttQualityOfServiceLevel.AtLeastOnce,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException(paramName: nameof(topic), message: "Topic must not be null.");
            if (payload == null || payload == default)
                throw new ArgumentNullException(paramName: nameof(payload), message: "Payload must not be null.");

            var messageBuilder = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag();

            switch (qualityOfService)
            {
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    messageBuilder.WithAtLeastOnceQoS();
                    break;
                case MqttQualityOfServiceLevel.AtMostOnce:
                    messageBuilder.WithAtMostOnceQoS();
                    break;
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    messageBuilder.WithExactlyOnceQoS();
                    break;
            }
            var message = messageBuilder.Build();

            await Client.ConnectAsync(Options, cancellationToken);
            await Client.PublishAsync(message, cancellationToken);
        }

        public void Dispose()
        {
            Client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
