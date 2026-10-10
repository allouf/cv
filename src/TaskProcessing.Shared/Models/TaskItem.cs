namespace TaskProcessing.Shared.Models;

public class TaskItem
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Status { get; set; } = "Pending";
    public string Result { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
