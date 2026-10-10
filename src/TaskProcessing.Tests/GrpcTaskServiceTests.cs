using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskProcessing.Api.Services;
using TaskProcessing.Shared;
using TaskProcessing.Shared.Messaging;
using TaskProcessing.Shared.Models;
using Xunit;

namespace TaskProcessing.Tests;

public class GrpcTaskServiceTests
{
    [Fact]
    public void SubmitTask_ReturnsTaskId()
    {
        var mockPublisher = new Mock<RabbitMqPublisher>(MockBehavior.Loose, new ConfigurationBuilder().Build());
        mockPublisher.Setup(p => p.Publish(It.IsAny<TaskItem>()));
        var mockLogger = new Mock<ILogger<GrpcTaskService>>();
        var service = new GrpcTaskService(mockPublisher.Object, mockLogger.Object);

        var request = new TaskRequest { Name = "test", Payload = "hello", Priority = 1 };
        var response = service.SubmitTask(request, new MockServerCallContext()).Result;

        Assert.False(string.IsNullOrEmpty(response.TaskId));
    }

    [Fact]
    public void SubmitTask_PublishesToRabbitMq()
    {
        var mockPublisher = new Mock<RabbitMqPublisher>(MockBehavior.Loose, new ConfigurationBuilder().Build());
        mockPublisher.Setup(p => p.Publish(It.IsAny<TaskItem>()));
        var mockLogger = new Mock<ILogger<GrpcTaskService>>();
        var service = new GrpcTaskService(mockPublisher.Object, mockLogger.Object);

        var request = new TaskRequest { Name = "order", Payload = "data123", Priority = 2 };
        service.SubmitTask(request, new MockServerCallContext()).Wait();

        mockPublisher.Verify(p => p.Publish(It.Is<TaskItem>(t => t.Name == "order" && t.Payload == "data123")), Times.Once);
    }
}

public class MockServerCallContext : ServerCallContext
{
    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    protected override string MethodCore => "test";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "127.0.0.1";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => new Metadata();
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => new Metadata();
    protected override Status StatusCore { get; set; } = new Status(StatusCode.OK, string.Empty);
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => null;
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => null;
}
