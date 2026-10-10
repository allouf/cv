using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using TaskProcessing.Shared.Models;

namespace TaskProcessing.Worker.Services;

public class DynamoDBService
{
    private readonly IAmazonDynamoDB _client;
    private const string TableName = "Tasks";

    public DynamoDBService(IConfiguration configuration)
    {
        var region = configuration["DynamoDB:Region"] ?? "us-east-1";
        var serviceUrl = configuration["DynamoDB:ServiceUrl"];

        var clientConfig = new AmazonDynamoDBConfig();
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            clientConfig.ServiceURL = serviceUrl;
        }
        else
        {
            clientConfig.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
        }

        _client = new AmazonDynamoDBClient(clientConfig);
    }

    public async Task SaveTaskAsync(TaskItem task)
    {
        await _client.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "TaskId", new AttributeValue { S = task.TaskId } },
                { "Name", new AttributeValue { S = task.Name } },
                { "Payload", new AttributeValue { S = task.Payload } },
                { "Priority", new AttributeValue { N = task.Priority.ToString() } },
                { "Status", new AttributeValue { S = task.Status } },
                { "Result", new AttributeValue { S = task.Result } },
                { "CreatedAt", new AttributeValue { S = task.CreatedAt.ToString("O") } },
                { "CompletedAt", task.CompletedAt.HasValue ? new AttributeValue { S = task.CompletedAt.Value.ToString("O") } : new AttributeValue { NULL = true } }
            }
        });
    }
}
