# Side Project: Task Processing Microservice

## Goal
Build a minimal but real .NET Core 8 system using gRPC, RabbitMQ, and DynamoDB. After completing this, you can legitimately claim all four technologies on your CV.

## Architecture

```
Client (gRPC) -> API Service (.NET 8) -> RabbitMQ -> Worker Service (.NET 8) -> DynamoDB
```

## Components

### 1. API Service (.NET 8 + gRPC)
- Exposes a gRPC endpoint `SubmitTask`
- Accepts a task (name, payload, priority)
- Publishes the task to RabbitMQ
- Returns a task ID immediately

### 2. Worker Service (.NET 8 + RabbitMQ)
- Consumes messages from RabbitMQ
- Processes the task (simulate work: e.g., transform payload, calculate something)
- Writes the result to DynamoDB
- Handles retries and dead-letter queue

### 3. DynamoDB Table
- `Tasks` table: taskID (PK), name, payload, status, result, createdAt, completedAt

## Project Structure

```
/TaskProcessing
  /TaskProcessing.Api          # gRPC API service
  /TaskProcessing.Worker       # RabbitMQ consumer
  /TaskProcessing.Shared       # Protos + shared models
  /TaskProcessing.Tests        # Unit tests
  docker-compose.yml           # RabbitMQ + DynamoDB Local
```

## Step-by-Step

### Step 1: Setup
```bash
dotnet new grpc -n TaskProcessing.Api
dotnet new worker -n TaskProcessing.Worker
dotnet new classlib -n TaskProcessing.Shared
```

### Step 2: Proto file (`Shared/Protos/task.proto`)
```protobuf
syntax = "proto3";
option csharp_namespace = "TaskProcessing.Shared";

package tasks;

service TaskService {
  rpc SubmitTask (TaskRequest) returns (TaskResponse);
  rpc GetTaskStatus (StatusRequest) returns (StatusResponse);
}

message TaskRequest {
  string name = 1;
  string payload = 2;
  int32 priority = 3;
}

message TaskResponse {
  string task_id = 1;
}

message StatusRequest {
  string task_id = 1;
}

message StatusResponse {
  string task_id = 1;
  string status = 2;
  string result = 3;
}
```

### Step 3: API Service — publish to RabbitMQ
- Use `RabbitMQ.Client` NuGet package
- Serialize task to JSON, publish to `tasks` queue

### Step 4: Worker Service — consume + write to DynamoDB
- Use `AWSSDK.DynamoDBv2` NuGet package
- Consume from `tasks` queue
- Process task, write result to DynamoDB

### Step 5: Docker Compose for local infra
```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
  dynamodb-local:
    image: amazon/dynamodb-local
    ports:
      - "8000:8000"
```

### Step 6: Unit Tests
- Test gRPC service logic
- Test worker processing logic
- Mock RabbitMQ and DynamoDB

## CV Bullets After Completion

- Designed and built a **distributed task processing microservice** using **.NET Core 8**, implementing **gRPC** for client-to-service communication and **RabbitMQ** for asynchronous message-based task distribution between services.
- Implemented a consumer worker that processes tasks from RabbitMQ with retry and dead-letter handling, persisting results to **DynamoDB** with a schema optimized for status queries.
- Built with Docker Compose for local development infrastructure (RabbitMQ + DynamoDB Local), with unit tests covering gRPC service logic and worker processing pipelines.

## Time Estimate
- 2-3 weekends for a working minimal version
- 1 more weekend for tests and polish

## What You'll Be Able to Talk About in Interviews
- Why gRPC over REST (strong typing, streaming, performance)
- Why RabbitMQ (decoupling, async processing, retry patterns)
- Why DynamoDB (serverless, key-value, high-write throughput)
- .NET Core 8 minimal APIs vs gRPC services
- Docker Compose for local microservice development
