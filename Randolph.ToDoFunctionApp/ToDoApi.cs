using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
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
        [Table(Constants.ToDoName)] IAsyncCollector<TodoTableEntity> tableClient,
        [Queue(Constants.ToDoName)] IAsyncCollector<ToDoModel> toDoQueue,
        ILogger log)
    {
        log.LogInformation("Adding an item to the ToDo list");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var input = JsonConvert.DeserializeObject<CreateToDoModel>(requestBody);
        var todo = new ToDoModel { ToDoId = Guid.NewGuid(), TaskDescription = input?.TaskDescription };
        var entity = this._mapper.Map<TodoTableEntity>(todo);
        
        await tableClient.AddAsync(entity);
        await toDoQueue.AddAsync(todo);

        return new CreatedAtRouteResult("GetAllTodos", todo);
    }
    
    [FunctionName(nameof(GetAllToDos))]
    public IActionResult GetAllToDos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req, 
        [Table(Constants.ToDoName)] TableClient tableClient,
        ILogger log)
    {
        log.LogInformation("Getting all ToDo's");
        
        var results = tableClient.QueryAllToModelAsync<TodoTableEntity, ToDoModel>(entity => this._mapper.Map<TodoTableEntity, ToDoModel>(entity));

        return new OkObjectResult(results);
    }
    
    [FunctionName(nameof(GetTodoById))]
    public async Task<IActionResult> GetTodoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,
        [Table(Constants.ToDoName)] TableClient tableClient,
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
        [Table(Constants.ToDoName)] TableClient tableClient,
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
        [Table(Constants.ToDoName)] TableClient tableClient,
        Guid id, 
        ILogger log)
    {
        log.LogInformation("Updating ToDo {Id}", id);

        var entity = await tableClient.QueryAsync<TodoTableEntity>(entity => entity.RowKey == id.ToString("n")).SingleOrDefaultAsync();

        if (entity == null)
        {
            return new NotFoundObjectResult(new { id });
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var todo = JsonConvert.DeserializeObject<UpdateToDoModel>(requestBody);
        var updatedEntity = this._mapper.Map<TodoTableEntity>(todo);
        updatedEntity.CreatedDt = entity.CreatedDt;
        updatedEntity.RowKey = entity.RowKey;
        updatedEntity.PartitionKey = Constants.PartitionKey;

        await tableClient.UpdateEntityAsync(updatedEntity, ETag.All);
        
        return new NoContentResult();
    }
    
    [FunctionName(nameof(DeleteToDo))]
    public async Task<IActionResult> DeleteToDo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest request,
        [Table(Constants.ToDoName)] TableClient tableClient,
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