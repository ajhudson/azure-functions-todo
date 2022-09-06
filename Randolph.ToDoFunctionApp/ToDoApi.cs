using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Randolph.ToDoFunctionApp;

public class ToDoApi
{
    private readonly IMapper _mapper;
    
    public ToDoApi(IMapper mapper)
    {
        this._mapper = mapper;
    }

    [FunctionName(nameof(CreateToDo))]
    public async Task<IActionResult> CreateToDo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
        [Table(Constants.ToDoTableName)] TableClient table,
        ILogger log)
    {
        log.LogInformation("Adding an item to the ToDo list");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<CreateToDoModel>(requestBody);
        var todo = new ToDoModel { TaskDescription = input?.TaskDescription };
        var entity = this._mapper.Map<TodoTableEntity>(todo);

        await table.AddEntityAsync(entity);

        return new CreatedAtRouteResult("GetAllTodos", todo);
    }

    /*
    [FunctionName(nameof(GetAllToDos))]
    public async Task<IActionResult> GetAllToDos([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req, ILogger log)
    {
        log.LogInformation("Getting all ToDo's");
        await Task.CompletedTask;

        return new OkObjectResult(_toDos);
    }

    [FunctionName(nameof(GetTodoById))]
    public async Task<IActionResult> GetTodoById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req, string id, ILogger log)
    {
        log.LogInformation("Getting an todo for {Result}", id);

        var todo = _toDos.SingleOrDefault(x => x.Id == id);
        await Task.CompletedTask;
        
        return todo != null ? new OkObjectResult(todo) : new NotFoundObjectResult(new { id });
    }

    [FunctionName(nameof(MarkToDoAsDone))]
    public async Task<IActionResult> MarkToDoAsDone([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo/{id}")]HttpRequest req, string id, ILogger log)
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
    public async Task<IActionResult> UpdateToDo([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")]HttpRequest req, string id, ILogger log)
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
    public async Task<IActionResult> DeleteToDo([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest request, string id, ILogger log)
    {
        log.LogInformation("Deleting ToDo {Id}", id);

        var todo = _toDos.SingleOrDefault(x => x.Id == id);

        await Task.CompletedTask;

        if (todo == null)
        {
            return new NotFoundObjectResult(new { id });
        }

        _toDos.Remove(todo);

        return new NoContentResult();
    }
    */
}