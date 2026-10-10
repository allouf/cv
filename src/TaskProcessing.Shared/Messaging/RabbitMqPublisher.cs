using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using TaskProcessing.Shared.Models;

namespace TaskProcessing.Shared.Messaging;

public class RabbitMqPublisher : IDisposable
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "tasks";
    private const string ExchangeName = "tasks.exchange";

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IModel GetChannel()
    {
        if (_channel is { IsOpen: true })
            return _channel;

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(QueueName, ExchangeName, routingKey: "task");

        return _channel;
    }

    public virtual void Publish(TaskItem task)
    {
        var channel = GetChannel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(task));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: "task",
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
