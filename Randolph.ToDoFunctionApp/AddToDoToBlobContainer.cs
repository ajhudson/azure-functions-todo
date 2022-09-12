using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Randolph.ToDoFunctionApp.Models;

namespace Randolph.ToDoFunctionApp;

public static class ToDos
{
    [FunctionName("AddToDoToBlobContainer")]
    public static async Task RunAsync(
        [QueueTrigger(Constants.ToDoName)] ToDoModel todo,
        [Blob(Constants.ToDoBlobContainerName, FileAccess.Write)] BlobContainerClient containerClient,
        ILogger log)
    {
        await containerClient.CreateIfNotExistsAsync();
        var blockBlockClient = containerClient.GetBlockBlobClient($"{todo.ToDoId:n}.txt");

        var textContentBytes = Encoding.UTF8.GetBytes($"Created new task {todo.ToDoId:N}");
        using (var memoryStream = new MemoryStream(textContentBytes))
        {
            using (var reader = new StreamReader(memoryStream))
            {
                await blockBlockClient.UploadAsync(reader.BaseStream);
            }
            
        }

        log.LogInformation("C# Queue trigger function processed: {@Todo}", todo);
    }
}