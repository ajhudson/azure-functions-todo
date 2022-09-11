using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.Extensions;
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
        [Table(Constants.ToDoTableName)] TableClient tableClient,
        ILogger log)
    {
        log.LogInformation("Adding an item to the ToDo list");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<CreateToDoModel>(requestBody);
        var todo = new ToDoModel { ToDoId = Guid.NewGuid(), TaskDescription = input?.TaskDescription };
        var entity = this._mapper.Map<TodoTableEntity>(todo);

        Response response = await tableClient.AddEntityAsync(entity);

        return new CreatedAtRouteResult("GetAllTodos", todo);
    }
    
    [FunctionName(nameof(GetAllToDos))]
    public IActionResult GetAllToDos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req, 
        [Table(Constants.ToDoTableName)] TableClient tableClient,
        ILogger log)
    {
        log.LogInformation("Getting all ToDo's");
        
        var results = tableClient.QueryAllToModelAsync<TodoTableEntity, ToDoModel>(entity => this._mapper.Map<TodoTableEntity, ToDoModel>(entity));

        return new OkObjectResult(results);
    }
    
    [FunctionName(nameof(GetTodoById))]
    public async Task<IActionResult> GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,
        [Table(Constants.ToDoTableName)] TableClient tableClient,
        Guid id, 
        ILogger log)
    {
        log.LogInformation("Getting an todo for {Result}", id);

        var todo = await tableClient.QueryAllToModelAsync<TodoTableEntity, ToDoModel>(entity => this._mapper.Map<TodoTableEntity, ToDoModel>(entity), e => e.RowKey == id.ToString("n")).SingleOrDefaultAsync();

        return todo != null ? new OkObjectResult(todo) : new NotFoundObjectResult(new { id });
    }
    
    [FunctionName(nameof(MarkToDoAsDone))]
    public async Task<IActionResult> MarkToDoAsDone(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo/{id}")]HttpRequest req,
        [Table(Constants.ToDoTableName)] TableClient tableClient,
        Guid id, 
        ILogger log)
    {
        log.LogInformation("Marking ToDo {Id} as completed", id);

        var entity = await tableClient.QueryAsync<TodoTableEntity>(entity => entity.RowKey == id.ToString("n")).SingleOrDefaultAsync();
        
        if (entity == null)
        {
            return new NotFoundObjectResult(new { id });
        }

        entity.IsCompleted = true;
        await tableClient.UpdateEntityAsync(entity, ETag.All);

        return new NoContentResult();
    }
    
    [FunctionName(nameof(UpdateToDo))]
    public async Task<IActionResult> UpdateToDo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")]HttpRequest req,
        [Table(Constants.ToDoTableName)] TableClient tableClient,
        Guid id, 
        ILogger log)
    {
        log.LogInformation("Updating ToDo {Id}", id);

        var entity = await tableClient.QueryAsync<TodoTableEntity>(entity => entity.RowKey == id.ToString("n")).SingleOrDefaultAsync();

        if (entity == null)
        {
            return new NotFoundObjectResult(new { id });
        }
        
        var todo = JsonSerializer.Deserialize<ToDoModel>(req.Body);
        var updatedEntity = this._mapper.Map<TodoTableEntity>(todo);
        updatedEntity.RowKey = entity.RowKey;

        await tableClient.UpdateEntityAsync(updatedEntity, ETag.All);
        
        return new NoContentResult();
    }
    
    [FunctionName(nameof(DeleteToDo))]
    public async Task<IActionResult> DeleteToDo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest request,
        [Table(Constants.ToDoTableName)] TableClient tableClient,
        Guid id, 
        ILogger log)
    {
        log.LogInformation("Deleting ToDo {Id}", id);

        var todo = await tableClient.QueryAsync<TodoTableEntity>(entity => entity.RowKey == id.ToString("n")).SingleOrDefaultAsync();

        if (todo == null)
        {
            return new NotFoundObjectResult(new { id });
        }

        await tableClient.DeleteEntityAsync(Constants.PartitionKey, id.ToString("n"));

        return new NoContentResult();
    }
}