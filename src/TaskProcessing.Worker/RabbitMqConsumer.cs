using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskProcessing.Shared.Models;
using TaskProcessing.Worker.Services;

namespace TaskProcessing.Worker;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly DynamoDBService _dynamoDb;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection _connection;
    private IModel _channel;
    private const string QueueName = "tasks";
    private const string DeadLetterQueue = "tasks.dlq";

    public RabbitMqConsumer(IConfiguration configuration, DynamoDBService dynamoDb, ILogger<RabbitMqConsumer> logger)
    {
        _configuration = configuration;
        _dynamoDb = dynamoDb;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", string.Empty },
            { "x-dead-letter-routing-key", DeadLetterQueue }
        });
        _channel.QueueDeclare(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var task = JsonSerializer.Deserialize<TaskItem>(body);

            if (task == null)
            {
                _channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            try
            {
                _logger.LogInformation("Processing task {TaskId}", task.TaskId);
                await ProcessTaskAsync(task);
                await _dynamoDb.SaveTaskAsync(task);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process task {TaskId}", task.TaskId);
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(QueueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    private async Task ProcessTaskAsync(TaskItem task)
    {
        await Task.Delay(1000);
        task.Status = "Completed";
        task.Result = $"Processed: {task.Payload.ToUpperInvariant()} at {DateTime.UtcNow:O}";
        task.CompletedAt = DateTime.UtcNow;
        _logger.LogInformation("Task {TaskId} completed", task.TaskId);
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}
