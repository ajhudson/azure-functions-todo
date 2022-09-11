using System.Linq.Expressions;
using AutoMapper;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.MappingProfiles;
using Randolph.ToDoFunctionApp.Models;
using Shouldly;
using Xunit.Abstractions;

namespace Randolph.ToDoFunctionApp.Tests;

public class ToDoApiTests : TestsBase
{
    private readonly Mock<HttpRequest> _httpRequest;

    private readonly Mock<TableClient> _tableClient;

    private readonly Mock<ILogger> _logger;
    
    private readonly ITestOutputHelper _output;

    private readonly ToDoApi _toDoApi;

    public ToDoApiTests(ITestOutputHelper output)
    {
        this._httpRequest = new Mock<HttpRequest>();
        this._tableClient = new Mock<TableClient>();
        this._logger = new Mock<ILogger>();
        this._output = output;

        var mappingConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ToDoMappingProfile>();
        });

        var mapper = mappingConfig.CreateMapper();
        this._toDoApi = new ToDoApi(mapper);
    }
    
    [Fact]
    public async Task ShouldAddTodo()
    {
        // Arrange
        var model = new CreateToDoModel { TaskDescription = "Do something nice" };
        var body = await CreateStreamForHttpRequest(model);
        
        this._httpRequest.Setup(x => x.Body).Returns(body);
        this._tableClient.Setup(x => x.AddEntityAsync(It.IsAny<TodoTableEntity>(), It.IsAny<CancellationToken>())).Verifiable();

        // Act
        var response = await this._toDoApi.CreateToDo(this._httpRequest.Object, this._tableClient.Object, _logger.Object) as CreatedAtRouteResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        
        this._tableClient.Verify(x => x.AddEntityAsync(It.IsAny<TodoTableEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        var responseModel = response.Value as ToDoModel;
        responseModel.ShouldNotBeNull();
        responseModel.ToDoId.ShouldNotBe(Guid.Empty);
        this._output.WriteLine($"ToDo created with ID of {responseModel.ToDoId}");
    }
    
    [Fact]
    public async Task ShouldGetAllToDos()
    {
        // Arrange
        const string TaskDescription = "Clean crap up";

        var id = Guid.NewGuid();
        
        var entity = new TodoTableEntity
        {
            RowKey = id.ToString("n"),
            PartitionKey = Constants.PartitionKey,
            CreatedDt = DateTime.UtcNow,
            IsCompleted = false,
            TaskDescription = TaskDescription
        };

        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(new[] { entity }, null, Mock.Of<Response>())
        });

        this._tableClient.Setup(x => x.QueryAsync<TodoTableEntity>(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        // Act
        var response = this._toDoApi.GetAllToDos(this._httpRequest.Object, this._tableClient.Object, this._logger.Object) as OkObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status200OK);

        var asyncToDos = response.Value as IAsyncEnumerable<ToDoModel>;
        var allToDos = await asyncToDos!.ToListAsync();
        allToDos.Count.ShouldBe(1);
        allToDos.Any(todo => todo.TaskDescription == TaskDescription).ShouldBeTrue();
    }
    
    [Fact]
    public async Task ShouldGetToDoById()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TodoTableEntity
        {
            RowKey = id.ToString("n"),
            CreatedDt = DateTime.UtcNow,
            TaskDescription = "Wake up",
            PartitionKey = Constants.PartitionKey
        };
    
        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(new[] { entity }, null, Mock.Of<Response>())
        });

        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);

        // Act
        var response = await this._toDoApi.GetTodoById(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as OkObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status200OK);

        var todo = response.Value as ToDoModel;
        todo.ShouldNotBeNull();
        todo.ToDoId.ShouldBe(id);
        todo.IsCompleted.ShouldBeFalse();
    }
    
    [Fact]
    public async Task ShouldMarkToDoAsDone()
    {
        // Arrange
        var id = Guid.NewGuid();

        var todo = new TodoTableEntity
        {
            CreatedDt = DateTime.UtcNow,
            PartitionKey = Constants.PartitionKey,
            TaskDescription = "This needs to be marked as done",
            RowKey = id.ToString("n")
        };

        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(new[] { todo }, null, Mock.Of<Response>())
        });

        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        this._tableClient.Setup(x => x.UpdateEntityAsync(It.Is<TodoTableEntity>(e => e.IsCompleted == true), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>())).Verifiable();


        // Act
        var response = await this._toDoApi.MarkToDoAsDone(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as NoContentResult;
        
        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
        
        this._tableClient.Verify(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        
        this._tableClient.Verify(x => x.UpdateEntityAsync(It.Is<TodoTableEntity>(e => e.IsCompleted == true), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ShouldReturnNotFoundForNonExistentToDo()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empty = Array.Empty<TodoTableEntity>().ToList();
        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(empty, null, Mock.Of<Response>())
        });

        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        this._tableClient.Setup(x => x.UpdateEntityAsync(It.Is<TodoTableEntity>(e => e.IsCompleted == true), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>())).Verifiable();

        
        // Act
        var response = await this._toDoApi.MarkToDoAsDone(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as NotFoundObjectResult;
        
        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        this._tableClient.Verify(x => x.UpdateEntityAsync(It.Is<TodoTableEntity>(e => e.IsCompleted == true), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task ShouldReturnNotFoundWhenUpdateToDoIsCalled()
    {
        // Arrange
        var id = Guid.NewGuid();

        var empty = Array.Empty<TodoTableEntity>().ToList();
        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(empty, null, Mock.Of<Response>())
        });

        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        this._tableClient.Setup(x => x.UpdateEntityAsync(It.Is<TodoTableEntity>(e => e.IsCompleted == true), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>())).Verifiable();
        
        
        // Act
        var response = await this._toDoApi.UpdateToDo(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as NotFoundObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        this._tableClient.Verify(x => x.UpdateEntityAsync(It.Is<TodoTableEntity>(e => e.IsCompleted == true), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task ShouldReturnNoContentWhenToDoIsUpdated()
    {
        // Arrange
        var id = Guid.NewGuid();

        var model = new ToDoModel { ToDoId = id, TaskDescription = "Updated ToDo" };
        var stream = await CreateStreamForHttpRequest(model);
        this._httpRequest.SetupGet(x => x.Body).Returns(stream);

        var toDoEntity = new TodoTableEntity
        {
            CreatedDt = DateTime.UtcNow,
            PartitionKey = Constants.PartitionKey,
            TaskDescription = "Original ToDo",
            RowKey = id.ToString("n")
        };

        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(new[] { toDoEntity }, null, Mock.Of<Response>())
        });
        
        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        this._tableClient.Setup(x => x.UpdateEntityAsync(It.IsAny<TodoTableEntity>(), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>())).Verifiable();


        // Act
        var response = await this._toDoApi.UpdateToDo(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as NoContentResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);    
        this._tableClient.Verify(x => x.UpdateEntityAsync(It.IsAny<TodoTableEntity>(), It.IsAny<ETag>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    
    [Fact]
    public async Task ShouldReturnNotFoundWhenDeleteToDoIsCalled()
    {
        // Arrange
        var id = Guid.NewGuid();
        var empty = Array.Empty<TodoTableEntity>().ToList();
        
        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(empty, null, Mock.Of<Response>())
        });
        
        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        this._tableClient.Setup(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ETag>(), It.IsAny<CancellationToken>())).Verifiable();

        // Act
        var response = await this._toDoApi.DeleteToDo(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as NotFoundObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        this._tableClient.Verify(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ETag>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task ShouldReturnNoContentWhenToDoIsDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        var todo = new ToDoModel { ToDoId = id, CreatedAt = DateTime.Now, TaskDescription = "De-clutter bookshelf" };

        var entities = new List<TodoTableEntity>
        {
            new() { RowKey = id.ToString("n"), CreatedDt = todo.CreatedAt, TaskDescription = todo.TaskDescription }
        };
        
        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(entities, null, Mock.Of<Response>())
        });
        
        this._tableClient.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        this._tableClient.Setup(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ETag>(), It.IsAny<CancellationToken>())).Verifiable();

        // Act
        var response = await this._toDoApi.DeleteToDo(this._httpRequest.Object, this._tableClient.Object, id, this._logger.Object) as NoContentResult;
        
        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
        this._tableClient.Verify(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ETag>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}