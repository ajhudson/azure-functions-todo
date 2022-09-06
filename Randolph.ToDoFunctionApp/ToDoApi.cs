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
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Randolph.ToDoFunctionApp;

public static class ToDoApi
{
    private static List<ToDoModel> _toDos = new();

    public static List<ToDoModel> ToDos
    {
        get => _toDos;

        set => _toDos = value;
    }

    [FunctionName(nameof(CreateToDo))]
    public static async Task<IActionResult> CreateToDo([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req, ILogger log)
    {
        log.LogInformation("Adding an item to the ToDo list");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonSerializer.Deserialize<CreateToDoModel>(requestBody);
        var todo = new ToDoModel { TaskDescription = input?.TaskDescription };
        _toDos.Add(todo);

        return new CreatedAtRouteResult(nameof(GetAllToDos), todo);
    }

    [FunctionName(nameof(GetAllToDos))]
    public static async Task<IActionResult> GetAllToDos([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req, ILogger log)
    {
        log.LogInformation("Getting all ToDo's");
        await Task.CompletedTask;

        return new OkObjectResult(_toDos);
    }

    [FunctionName(nameof(GetTodoById))]
    public static async Task<IActionResult> GetTodoById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req, string id, ILogger log)
    {
        log.LogInformation("Getting an todo for {Result}", id);

        var todo = _toDos.SingleOrDefault(x => x.Id == id);
        await Task.CompletedTask;
        
        return todo != null ? new OkObjectResult(todo) : new NotFoundObjectResult(new { id });
    }

    [FunctionName(nameof(MarkToDoAsDone))]
    public static async Task<IActionResult> MarkToDoAsDone([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo/{id}")]HttpRequest req, string id, ILogger log)
    {
        log.LogInformation("Marking ToDo {Id} as completed", id);

        var idx = _toDos.FindIndex(x => x.Id == id);

        if (idx == -1)
        {
            return new NotFoundObjectResult(new { id });
        }

        _toDos[idx].IsCompleted = true;

        await Task.CompletedTask;

        return new NoContentResult();
    }

    [FunctionName(nameof(UpdateToDo))]
    public static async Task<IActionResult> UpdateToDo([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")]HttpRequest req, string id, ILogger log)
    {
        log.LogInformation("Updating ToDo {Id}", id);

        var idx = _toDos.FindIndex(x => x.Id == id);

        if (idx == -1)
        {
            return new NotFoundObjectResult(new { id });
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var todo = JsonSerializer.Deserialize<ToDoModel>(requestBody);

        _toDos[idx] = todo;
        
        return new NoContentResult();
    }

    [FunctionName(nameof(DeleteToDo))]
    public static async Task<IActionResult> DeleteToDo([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest request, string id, ILogger log)
    {
        log.LogInformation("Deleting ToDo {Id}", id);

        var todo = _toDos.SingleOrDefault(x => x.Id == id);

        if (todo == null)
        {
            return new NotFoundObjectResult(new { id });
        }

        _toDos.Remove(todo);

        return new NoContentResult();
    }
}