using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.Extensions;
using Randolph.ToDoFunctionApp.Models;

namespace Randolph.ToDoFunctionApp;

public static class DeleteCompletedTasksTimerFunction
{
    [FunctionName("DeleteCompletedTasksTimerFunction")]
    public static async Task RunAsync(
        [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
        [Table(Constants.ToDoName)] TableClient tableClient,
        ILogger log)
    {
        log.LogInformation("C# Timer trigger function executed at: {UtcNow}", DateTime.UtcNow);

        var completedTasks = tableClient.QueryAsync<TodoTableEntity>(todo => todo.IsCompleted == true);

        await foreach (var currentPage in completedTasks.AsPages())
        {
            foreach (var currentEntity in currentPage.Values)
            {
                log.LogInformation("Deleting task {RowKey}", currentEntity.RowKey);
                await tableClient.DeleteEntityAsync(Constants.PartitionKey, currentEntity.RowKey);
            }
        }
    }
}