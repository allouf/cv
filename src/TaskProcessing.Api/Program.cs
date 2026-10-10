using TaskProcessing.Api.Services;
using TaskProcessing.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

app.MapGrpcService<GrpcTaskService>();
app.MapGet("/", () => "Task Processing API — gRPC endpoint available at /tasks");

app.Run();
