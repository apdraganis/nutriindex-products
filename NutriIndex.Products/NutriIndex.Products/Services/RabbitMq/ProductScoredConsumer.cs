using NutriIndex.Products.Models.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NutriIndex.Products.Services.RabbitMq;

public class ProductScoredConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductScoredConsumer> _logger;
    private readonly string _hostname = "rabbitmq";
    private readonly string _queueName = "product.scored";

    private IConnection? _connection;
    private IChannel? _channel;

    public ProductScoredConsumer(IServiceProvider serviceProvider, ILogger<ProductScoredConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting ProductScoredConsumer background listener...");

        try
        {
            var factory = new ConnectionFactory { HostName = _hostname };

            // 1. Establish async connection and channel
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // 2. Ensure the scored queue exists
            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // 3. Create the asynchronous consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, @event) =>
            {
                try
                {
                    var body = @event.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received ProductScoredEvent from RabbitMQ");

                    // Use Web-style defaults (automatically configures camelCase parsing)???
                    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

                    // 4. Deserialize the message
                    var scoredEvent = JsonSerializer.Deserialize<ProductScoredEvent>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Validate the incoming payload before touching db.
                    if (scoredEvent == null || string.IsNullOrWhiteSpace(scoredEvent.Barcode))
                    {
                        _logger.LogWarning("Discarding malformed message. Raw payload: {RawMessage}", message);

                        // Acknowledge so it's removed from the queue and doesn't loop forever
                        await _channel.BasicAckAsync(@event.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        return;
                    }

                    // 5. Create a temporary DI scope to resolve the EF Core DB services safely
                    using var scope = _serviceProvider.CreateScope();
                    var catalogService = scope.ServiceProvider.GetRequiredService<ProductCatalogService>();

                    await catalogService.HandleProductScoredAsync(scoredEvent);

                    // 6. Acknowledge the message (remove it from queue)
                    await _channel.BasicAckAsync(@event.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ProductScoredEvent. Message kept on queue.");

                    // In production, publish to a Dead Letter Queue.
                    // For local MVP I reject it WITHOUT requeuing to prevent infinite loops.
                    await _channel.BasicNackAsync(@event.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            // 7. Start listening to the queue
            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false, // Explicit ACKs prevent message loss
                consumer: consumer,
                cancellationToken: stoppingToken);

            // Keep the background service alive while the app runs
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ProductScoredConsumer shutting down gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "ProductScoredConsumer failed to initialize RabbitMQ connection.");
        }
    }
}
