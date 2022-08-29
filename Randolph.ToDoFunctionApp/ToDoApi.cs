using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Randolph.ToDoFunctionApp.Models;

namespace Randolph.ToDoFunctionApp;

public static class ToDoApi
{
    private static List<ToDoModel> ToDos = new();

    [FunctionName(nameof(CreateToDo))]
    public static async Task<IActionResult> CreateToDo([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req, ILogger log)
    {
        log.LogInformation("Adding an item to the ToDo list");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<CreateToDoModel>(requestBody);
        var todo = new ToDoModel { TaskDescription = input.TaskDescription };
        ToDos.Add(todo);

        return new CreatedAtRouteResult(nameof(GetAllToDos), todo);
    }

    [FunctionName(nameof(GetAllToDos))]
    public static async Task<IActionResult> GetAllToDos([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req, ILogger log)
    {
        log.LogInformation("Getting all ToDo's");
        await Task.CompletedTask;

        return new OkObjectResult(ToDos);
    }

    [FunctionName(nameof(GetTodoById))]
    public static async Task<IActionResult> GetTodoById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req, string id, ILogger log)
    {
        log.LogInformation("Getting an todo for {Result}", id);

        var todo = ToDos.SingleOrDefault(x => x.Id == id);
        await Task.CompletedTask;
        
        return todo != null ? new OkObjectResult(todo) : new NotFoundObjectResult(new { id });
    }

    [FunctionName(nameof(MarkToDoAsDone))]
    public static async Task<IActionResult> MarkToDoAsDone([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo/{id}")]HttpRequest req, string id, ILogger log)
    {
        log.LogInformation("Marking ToDo {Id} as completed", id);

        var idx = ToDos.FindIndex(x => x.Id == id);

        if (idx == -1)
        {
            return new NotFoundObjectResult(new { id });
        }

        ToDos[idx].IsCompleted = true;

        await Task.CompletedTask;

        return new NoContentResult();
    }
}