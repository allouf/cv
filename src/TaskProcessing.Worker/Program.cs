using TaskProcessing.Worker;
using TaskProcessing.Worker.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<DynamoDBService>();
        services.AddHostedService<RabbitMqConsumer>();
    })
    .Build();

host.Run();
