using JobsParser.Core.Abstractions;
using JobsParser.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace JobParsers.Infrastructure.Queue
{
    public class RabbitMqService : IQueueService, IAsyncDisposable
    {
        private readonly RabbitSettings _rabbitSettings;
        private readonly ILogger<RabbitMqService> _logger;
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMqService(IOptions<RabbitSettings> rabbitSettings, ILogger<RabbitMqService> logger)
        {
            ArgumentNullException.ThrowIfNull(rabbitSettings);

            _rabbitSettings = rabbitSettings.Value;
            _logger = logger;

            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult(); // Ensure initialization completes
        }

        private async Task InitializeAsync()
        {
            try
            {
                var factory = new ConnectionFactory { HostName = _rabbitSettings.HostName };
                _connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("RabbitMQ connection established.");

                _channel = await _connection.CreateChannelAsync();
                _logger.LogInformation("RabbitMQ channel created.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ connection and channel.");
                throw;
            }
        }

        public async Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _logger.LogError("RabbitMQ connection is not open. Cannot publish message.");
                return;
            }

            await EnsureQueueDeclaredAsync(queueName); // Ensure queue is declared 

            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true
                };
                await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: properties, body: body, cancellationToken: cancellationToken);

                _logger.LogInformation($"Message published to queue '{queueName}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing message to queue '{queueName}'.");
            }
        }

        public async Task ConsumeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default)
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _logger.LogError("RabbitMQ connection is not open. Cannot consume message.");
                return;
            }
            await EnsureQueueDeclaredAsync(queueName); // Ensure queue is declared

            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);  // Create consumer
                consumer.ReceivedAsync += async (model, ea) =>  // Use synchronous event handler
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var obj = JsonSerializer.Deserialize<T>(message);

                        if (obj != null)
                        {
                            try
                            {
                                await handler(obj);  // Asynchronously handle message
                                await _channel.BasicAckAsync(ea.DeliveryTag, false);  // Acknowledge message
                                _logger.LogDebug($"Message processed and acknowledged from queue '{queueName}'.");
                            }
                            catch (Exception handleEx)
                            {
                                _logger.LogError(handleEx, $"Exception during handler execution for message from queue '{queueName}'.");
                                //await _channel.BasicNackAsync(ea.DeliveryTag, false, true); // Requeue message if processing fails
                                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);  // Reject message
                            }
                        }
                        else
                        {
                            _logger.LogError($"Could not deserialize message from queue '{queueName}'.");
                            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);  // Reject message
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing message from queue '{queueName}'.");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);  // Reject message
                    }
                };

                string consumerTag = await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);  // Start consuming

                _logger.LogInformation($"Consuming messages from queue '{queueName}' with consumer tag '{consumerTag}'.");

                cancellationToken.Register(async () => // Register cancellation callback
                {
                    try
                    {
                        await _channel.BasicCancelAsync(consumerTag);  // Cancel consuming
                        _logger.LogInformation($"Consumer with tag '{consumerTag}' cancelled for queue '{queueName}'.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error during consumer cancellation for queue '{queueName}'.");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting up consumer for queue '{queueName}'.");
            }
        }

        private async Task EnsureQueueDeclaredAsync(string queueName)
        {
            try
            {
                // Declare DLX (Dead Letter Exchange)
                await _channel.ExchangeDeclareAsync("dlx_exchange", ExchangeType.Direct, durable: true);

                var args = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", "dlx_exchange" } // Ustawienie Dead Letter Exchange
                };

                // Declare Dead Letter Queue (DLQ)
                await _channel.QueueDeclareAsync("dlq-queue", durable: true, exclusive: false, autoDelete: false);
                await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

                await _channel.QueueBindAsync("dlq-queue", "dlx_exchange", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error declaring queue '{queueName}'.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
            {
                try
                {
                    await _channel.CloseAsync();
                    _logger.LogInformation("RabbitMQ channel closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing RabbitMQ channel.");
                }
                finally
                {
                    _channel.Dispose();
                }
            }

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                    _logger.LogInformation("RabbitMQ connection closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing RabbitMQ connection.");
                }
                finally
                {
                    _connection.Dispose();
                }
            }

            _logger.LogInformation("RabbitMQ resources disposed.");
        }
    }
}
