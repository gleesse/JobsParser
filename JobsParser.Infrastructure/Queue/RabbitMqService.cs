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
        private readonly RabbitSettings _settings;
        private readonly ILogger<RabbitMqService> _logger;
        private IConnection _connection;
        private IChannel _channel;

        public RabbitMqService(IOptions<RabbitSettings> settings, ILogger<RabbitMqService> logger)
        {
            if (settings == default) throw new ArgumentNullException(nameof(settings));

            _settings = settings.Value;
            _logger = logger;

            InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var factory = new ConnectionFactory { HostName = _settings.HostName };
                _connection = await factory.CreateConnectionAsync();
                _logger.LogInformation("RabbitMQ connection established.");

                _channel = await _connection.CreateChannelAsync();
                _logger.LogInformation("RabbitMQ channel created.");

                await SetupMessageInfrastructureAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ connection and channel.");
                throw;
            }
        }

        private async Task SetupMessageInfrastructureAsync()
        {
            try
            {
                if (_settings.EnableRetries)
                {
                    await SetupDelayedExchangeAsync();
                    await SetupQueuesAsync();
                    await SetupBindingsAsync();
                }

                _logger.LogInformation("RabbitMQ message infrastructure set up successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up RabbitMQ message infrastructure. Make sure the RabbitMQ Delayed Message Plugin is installed.");
            }
        }

        private async Task SetupDelayedExchangeAsync()
        {
            // Declare the x-delayed-message exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.RetryExchange,
                type: "x-delayed-message",
                durable: true,
                autoDelete: false,
                arguments: new Dictionary<string, object> { { "x-delayed-type", "direct" } });
        }

        private async Task SetupQueuesAsync()
        {
            // Declare the retry DLQ
            await _channel.QueueDeclareAsync(
                queue: _settings.RetryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Declare the failed DLQ
            await _channel.QueueDeclareAsync(
                queue: _settings.FailedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        private async Task SetupBindingsAsync()
        {
            // Bind the retry DLQ to the delayed exchange
            await _channel.QueueBindAsync(
                queue: _settings.RetryQueue,
                exchange: _settings.RetryExchange,
                routingKey: _settings.RetryQueue);

            // Bind the failed DLQ to the delayed exchange
            await _channel.QueueBindAsync(
                queue: _settings.FailedQueue,
                exchange: _settings.RetryExchange,
                routingKey: _settings.FailedQueue);
        }

        public async Task PublishAsync<T>(string queueName, T message)
        {
            EnsureConnectionIsOpen();
            await EnsureQueueDeclaredAsync(queueName);

            try
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                // In v7.0.0, BasicProperties is now a class, not an interface
                var properties = new BasicProperties
                {
                    Persistent = true,
                    Headers = _settings.EnableRetries
                        ? new Dictionary<string, object> { { "x-retry-count", "0" } }
                        : null
                };

                await PublishToExchangeAsync(queueName, properties, body);
                _logger.LogInformation($"Message published to queue '{queueName}' via exchange.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing message to queue '{queueName}'.");
            }
        }

        private void EnsureConnectionIsOpen()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                throw new InvalidOperationException("RabbitMQ connection is not open. Cannot perform operation.");
            }
        }

        private async Task PublishToExchangeAsync(string queueName, BasicProperties properties, byte[] body)
        {
            string exchange = _settings.EnableRetries ? _settings.RetryExchange : string.Empty;

            await _channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: queueName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        }

        public async Task ConsumeAsync<T>(string queueName, Func<T, Task> handler)
        {
            EnsureConnectionIsOpen();
            await EnsureQueueDeclaredAsync(queueName);

            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) => await HandleReceivedMessageAsync(queueName, handler, ea);

                string consumerTag = await _channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation($"Consuming messages from queue '{queueName}' with consumer tag '{consumerTag}'.");

                RegisterCancellationCallback(queueName, consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting up consumer for queue '{queueName}'.");
            }
        }

        private async Task HandleReceivedMessageAsync<T>(string queueName, Func<T, Task> handler, BasicDeliverEventArgs ea)
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var obj = JsonSerializer.Deserialize<T>(message);

                if (obj != null)
                {
                    await ProcessMessageAsync(queueName, handler, ea, obj, body);
                }
                else
                {
                    await HandleDeserializationFailureAsync(queueName, ea, body);
                }
            }
            catch (Exception ex)
            {
                await HandleProcessingExceptionAsync(queueName, ea, ex);
            }
        }

        private async Task ProcessMessageAsync<T>(string queueName, Func<T, Task> handler, BasicDeliverEventArgs ea, T obj, byte[] body)
        {
            try
            {
                await handler(obj);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogDebug($"Message processed and acknowledged from queue '{queueName}'.");
            }
            catch (Exception ex)
            {
                await HandleMessageProcessingFailureAsync(queueName, ea, ex, body);
            }
        }

        private async Task HandleMessageProcessingFailureAsync(string queueName, BasicDeliverEventArgs ea, Exception ex, byte[] body)
        {
            _logger.LogError(ex, $"Exception during handler execution for message from queue '{queueName}'.");

            if (_settings.EnableRetries)
            {
                var (retryCount, properties) = ExtractMessageMetadata(ea);

                if (ShouldRetryMessage(retryCount))
                {
                    await SendForRetryAsync(queueName, body, retryCount + 1, properties);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
                    await SendToFailedQueueAsync(queueName, body, retryCount, "Max retries exceeded", properties);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
            else
            {
                // Without retries, just reject the message
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        }

        private (int retryCount, BasicProperties properties) ExtractMessageMetadata(BasicDeliverEventArgs ea)
        {
            // Create new properties for the message
            var properties = new BasicProperties
            {
                Persistent = true
            };

            // Copy headers if they exist
            if (ea.BasicProperties.Headers != null)
            {
                properties.Headers = new Dictionary<string, object>(ea.BasicProperties.Headers);
            }
            else
            {
                properties.Headers = new Dictionary<string, object>();
            }

            // Get current retry count
            int retryCount = 0;
            if (properties.Headers.TryGetValue("x-retry-count", out var retryCountObj) &&
                retryCountObj is byte[] retryCountBytes)
            {
                retryCount = int.Parse(Encoding.UTF8.GetString(retryCountBytes));
            }

            return (retryCount, properties);
        }

        private bool ShouldRetryMessage(int retryCount)
        {
            return retryCount < _settings.MaxRetries;
        }

        private async Task SendForRetryAsync(string queueName, byte[] body, int retryCount, BasicProperties properties)
        {
            int delayMs = CalculateDelayMs();

            properties.Headers["x-retry-count"] = retryCount.ToString();
            properties.Headers["x-original-queue"] = queueName;
            properties.Headers["x-delay"] = delayMs;

            await _channel.BasicPublishAsync(
                exchange: _settings.RetryExchange,
                routingKey: queueName,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogInformation($"Message scheduled for retry {retryCount}/{_settings.MaxRetries} to queue '{queueName}' after {delayMs / 1000.0}s delay");
        }

        private async Task SendToFailedQueueAsync(string queueName, byte[] body, int retryCount, string reason, BasicProperties properties)
        {
            properties.Headers["x-retry-count"] = retryCount.ToString();
            properties.Headers["x-original-queue"] = queueName;
            properties.Headers["x-failure-reason"] = reason;

            await _channel.BasicPublishAsync(
                exchange: _settings.RetryExchange,
                routingKey: _settings.FailedQueue,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogWarning($"Message sent to failed queue. Reason: {reason}");
        }

        private async Task HandleDeserializationFailureAsync(string queueName, BasicDeliverEventArgs ea, byte[] body)
        {
            _logger.LogError($"Could not deserialize message from queue '{queueName}'.");

            if (_settings.EnableRetries)
            {
                var properties = new BasicProperties
                {
                    Persistent = true,
                    Headers = new Dictionary<string, object>()
                };

                await SendToFailedQueueAsync(queueName, body, 0, "Deserialization failed", properties);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        }

        private async Task HandleProcessingExceptionAsync(string queueName, BasicDeliverEventArgs ea, Exception ex)
        {
            _logger.LogError(ex, $"Error processing message from queue '{queueName}'.");
            await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
        }

        private void RegisterCancellationCallback(string queueName, string consumerTag)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.Register(async () =>
            {
                try
                {
                    await _channel.BasicCancelAsync(consumerTag);
                    _logger.LogInformation($"Consumer with tag '{consumerTag}' cancelled for queue '{queueName}'.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during consumer cancellation for queue '{queueName}'.");
                }
            });
        }

        private int CalculateDelayMs()
        {
            // Use configured delay
            int delayMs = _settings.RetryDelayMinutes * 60000;

            // Add some random jitter to prevent message bursts (±10% variance)
            int jitterMs = (int)(delayMs * 0.1 * (new Random().NextDouble() - 0.5));
            delayMs += jitterMs;

            return delayMs;
        }

        private async Task EnsureQueueDeclaredAsync(string queueName)
        {
            try
            {
                // Declare the queue
                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // If retries are enabled, bind the queue to the delayed exchange
                if (_settings.EnableRetries)
                {
                    await _channel.QueueBindAsync(
                        queue: queueName,
                        exchange: _settings.RetryExchange,
                        routingKey: queueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error declaring queue '{queueName}'.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeChannelAsync();
            await DisposeConnectionAsync();
            _logger.LogInformation("RabbitMQ resources disposed.");
        }

        private async Task DisposeChannelAsync()
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
                    await _channel.DisposeAsync();
                }
            }
        }

        private async Task DisposeConnectionAsync()
        {
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
        }
    }
}
