#region
// Requires package:
// <PackageReference Include="RabbitMQ.Client"/>
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Namespace
{
    /// <summary>
    /// Model responsible for holding a rabbit connection info.
    /// </summary>
    public sealed class RabbitConnection
    {
        /// <summary>
        /// RabbitMQ host to connect to.
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Port where Rabbit is available.
        /// </summary>
        /// <value>If none was given uses <see cref="AmqpTcpEndpoint.UseDefaultPort"/>.</value>
        public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;
        /// <summary>
        /// RabbitMQ connection password.
        /// </summary>
        /// <value>If none was given uses rabbit default "guest".</value>
        public string Password { get; set; } = "guest";
        /// <summary>
        /// RabbitMQ connection user.
        /// </summary>
        /// <value>If none was given uses rabbit default "guest".</value>
        public string Username { get; set; } = "guest";
    }

    /// <summary>
    /// Model responsible for holding a rabbit queue info.
    /// </summary>
    public sealed class RabbitQueue
    {
        /// <summary>
        /// Queue name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Set if a queue is still alive clearing messages.
        /// </summary>
        /// <value>Default value is true.</value>
        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; }
        public bool Exclusive { get; set; }
    }

    /// <summary>
    /// Model responsible for holding a rabbit publisher info.
    /// </summary>
    public sealed class RabbitPublisher
    {
        public string RoutingKey { get; set; } = "";
        public string Exchange { get; set; } = "";
        public IBasicProperties BasicProperties { get; set; }
    }

    /// <summary>
    /// Basic handler for receiving and publishing messages to RabbitMQ.
    /// </summary>
    public sealed class RabbitHandler
    {
        private ConnectionFactory ConnectionFactory { get; }

        /// <summary>
        /// Constructor responsible for creating a connection using <see cref="RabbitConnection"/>.
        /// </summary>
        /// <param name="rabbit">Rabbit connection information.</param>
        public RabbitHandler(RabbitConnection rabbit)
        {
            ConnectionFactory = new ConnectionFactory()
            {
                HostName = rabbit.Host,
                Port = rabbit.Port,
                UserName = rabbit.Username,
                Password = rabbit.Password
            };
        }

        /// <summary>
        /// Default channel basic properties.
        /// </summary>
        /// <remarks>Messages are set to be persistent.</remarks>
        public IBasicProperties BasicProperties
        {
            get
            {
                using (var conn = ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    return properties;
                }
            }
        }

        /// <summary>
        /// Sends a message to the RabbitMQ broker.
        /// </summary>
        /// <remarks>
        /// If <see cref="RabbitPublisher.BasicProperties"/> is not set uses <see cref="BasicProperties"/> as default.
        /// </remarks>
        /// <param name="messageBody">The message to be sent.</param>
        /// <param name="publisher">Publisher information.</param>
        /// <param name="queue">Queue information.</param>
        /// <example>
        /// <code>
        /// ...
        /// var body = Encoding.UTF8.GetBytes(jsonItem);
        /// handler.SendMessage(body, publisher, queue);
        /// ...
        /// </code>
        /// </example>
        public void SendMessage(
            byte[] messageBody,
            RabbitPublisher publisher,
            RabbitQueue queue)
        {
            if (messageBody == default)
                throw new ArgumentNullException(nameof(messageBody), "Message must not be null");
            if (publisher == null)
                throw new ArgumentNullException(nameof(publisher), "Publisher must not be null.");
            if (queue == null)
                throw new ArgumentNullException(nameof(queue), "Queue must not be null.");

            using (var conn = ConnectionFactory.CreateConnection())
            using (var channel = conn.CreateModel())
            {
                channel.QueueDeclare(queue: queue.Name,
                                    durable: queue.Durable,
                                    exclusive: queue.Exclusive,
                                    autoDelete: queue.AutoDelete);

                channel.BasicPublish(exchange: publisher.Exchange,
                                    routingKey: publisher.RoutingKey,
                                    basicProperties: publisher.BasicProperties ?? this.BasicProperties,
                                    messageBody);
            }
        }

        /// <summary>
        /// Initiates a new thread for receiving messages.
        /// </summary>
        /// <remarks>
        /// <para>Automatically handles acknowledged and not acknowledged responses.</para>
        /// <para>Use <paramref name="messageReceived"/> to manipulate the message.</para>
        /// <para>Use <paramref name="exceptionReceived"/> to manipulate exceptions.</para>
        /// </remarks>
        /// <param name="queue">Queue information.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="messageReceived">
        /// <para>Message received treatment.</para>
        /// <para>Usually message content is inside <see cref="BasicDeliverEventArgs.Body"/>.</para>
        /// </param>
        /// <param name="exceptionReceived">Exception treatment.</param>
        /// <param name="dieOnException">If an exception is thrown should the worker dies.</param>
        /// <example>
        /// <code>
        /// ...
        /// handler.ReceiveMessages(queue, cToken,
        ///     messageReceived: (model, ea) =>
        ///     {
        ///         var body = ea.Body.ToArray();
        ///         string jsonContent = Encoding.UTF8.GetString(body);
        ///         ...
        ///     },
        ///     exceptionReceived: (model, ea, exc) =>
        ///     {
        ///         logger.LogError("Something happened", exc);
        ///         ...
        ///     });
        /// ...
        /// </code>
        /// </example>
        public void ReceiveMessages(
            RabbitQueue queue,
            CancellationTokenSource cancellationToken,
            Action<object, BasicDeliverEventArgs> messageReceived,
            Action<object, BasicDeliverEventArgs, Exception> exceptionReceived = null,
            bool dieOnException = false)
        {
            if (queue == null)
                throw new ArgumentNullException(paramName: nameof(queue), message: "Queue information must not be null.");
            if (cancellationToken == default)
                throw new ArgumentNullException(paramName: nameof(cancellationToken), message: "Cancellation token must not be null.");
            if (messageReceived == null)
                throw new ArgumentNullException(paramName: nameof(messageReceived), message: "Callback must not be null.");

            Task.Factory.StartNew(() =>
            {
                using (var conn = ConnectionFactory.CreateConnection())
                using (var channel = conn.CreateModel())
                {
                    // maximum of 2 messages for each worker to receive.
                    channel.BasicQos(0, 2, false);

                    channel.QueueDeclare(queue: queue.Name,
                                        durable: queue.Durable,
                                        exclusive: queue.Exclusive,
                                        autoDelete: queue.AutoDelete);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, evtArgs) =>
                    {
                        try
                        {
                            messageReceived.Invoke(model, evtArgs);
                            channel.BasicAck(evtArgs.DeliveryTag, false);
                        }
                        catch (Exception e)
                        {
                            channel.BasicNack(evtArgs.DeliveryTag, false, true);
                            exceptionReceived?.Invoke(model, evtArgs, e);
                            if (dieOnException)
                            {
                                cancellationToken.Cancel();
                                // TODO: find a better way to stop the thread instantaneosly
                                // and not waiting for it to finish.
                                Task.Delay(1 * 1000).Wait();
                            }
                        }
                    };

                    channel.BasicConsume(queue: queue.Name,
                                        autoAck: false,
                                        consumer: consumer);

                    // keep thread alive
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Task.Delay(1 * 1000).Wait();
                    }
                }
            }, cancellationToken.Token);
        }
    }
}
