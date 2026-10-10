using Grpc.Core;
using TaskProcessing.Shared;
using TaskProcessing.Shared.Messaging;
using TaskProcessing.Shared.Models;

namespace TaskProcessing.Api.Services;

public class GrpcTaskService : TaskService.TaskServiceBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<GrpcTaskService> _logger;

    public GrpcTaskService(RabbitMqPublisher publisher, ILogger<GrpcTaskService> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public override Task<TaskResponse> SubmitTask(TaskRequest request, ServerCallContext context)
    {
        var task = new TaskItem
        {
            Name = request.Name,
            Payload = request.Payload,
            Priority = request.Priority,
            Status = "Queued"
        };

        _logger.LogInformation("Submitting task {TaskId} with priority {Priority}", task.TaskId, task.Priority);
        _publisher.Publish(task);

        return Task.FromResult(new TaskResponse { TaskId = task.TaskId });
    }

    public override Task<StatusResponse> GetTaskStatus(StatusRequest request, ServerCallContext context)
    {
        return Task.FromResult(new StatusResponse
        {
            TaskId = request.TaskId,
            Status = "Pending",
            Result = string.Empty
        });
    }
}
