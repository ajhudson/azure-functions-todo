namespace Randolph.ToDoFunctionApp;

public static class Constants
{
    public const string ToDoName = "ToDo";

    public const string ToDoBlobContainerName = "todos";

    public const string PartitionKey = "TODO";
    
    public const string AzureWebJobsStorage = nameof(AzureWebJobsStorage);
}