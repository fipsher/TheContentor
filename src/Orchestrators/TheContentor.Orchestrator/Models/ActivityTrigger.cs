namespace TheContentor.Orchestrator.Models;

public class ActivityTrigger<T>
{
    public required string InstanceId { get; set; }
    public required T Input { get; set; }
}