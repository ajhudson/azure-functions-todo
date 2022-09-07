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
using Randolph.ToDoFunctionApp.Tests.CustomAttributes;
using Shouldly;
using Xunit.Abstractions;

namespace Randolph.ToDoFunctionApp.Tests;

[TestCaseOrderer("Randolph.ToDoFunctionApp.Tests.UnitTestOrderers.AscendingNumberOrderer", "Randolph.ToDoFunctionApp.Tests")]
public class ToDoApiTests : TestsBase, IClassFixture<IdFixture>
{
    private readonly Mock<HttpRequest> _httpRequest;

    private readonly Mock<TableClient> _tableClient;

    private readonly Mock<ILogger> _logger;
    
    private readonly ITestOutputHelper _output;

    private readonly IdFixture _idFixture;

    private readonly ToDoApi _toDoApi;

    public ToDoApiTests(ITestOutputHelper output, IdFixture idFixture)
    {
        this._httpRequest = new Mock<HttpRequest>();
        this._tableClient = new Mock<TableClient>();
        this._logger = new Mock<ILogger>();
        this._output = output;
        this._idFixture = idFixture;

        var mappingConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ToDoMappingProfile>();
        });

        var mapper = mappingConfig.CreateMapper();
        this._toDoApi = new ToDoApi(mapper);
    }
    
    [Fact]
    [OrderTestByAscendingNumber(1)]
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
        this._idFixture.Id = responseModel.Id;
        this._output.WriteLine($"ToDo created with ID of {responseModel.Id}");
    }
    
    [Fact]
    [OrderTestByAscendingNumber(2)]
    public async Task ShouldGetAllToDos()
    {
        // Arrange
        var entity = new TodoTableEntity
        {
            PartitionKey = Constants.PartitionKey,
            CreatedDt = DateTime.UtcNow,
            IsCompleted = false,
            TaskDescription = "Clean crap up"
        };

        var results = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(new[] { entity }, null, Mock.Of<Response>())
        });

        this._tableClient.Setup(x => x.QueryAsync<TodoTableEntity>(It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(results);
        
        // Act
        var response = await this._toDoApi.GetAllToDos(this._httpRequest.Object, this._tableClient.Object, this._logger.Object) as OkObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        
        var allToDos = response.Value as List<ToDoModel>;
        allToDos.ShouldNotBeNull();
        allToDos.Count.ShouldBe(1);
        allToDos.Any(todo => todo.Id == this._idFixture.Id).ShouldBeTrue();
    }

    /*
    [Fact]
    [OrderTestByAscendingNumber(3)]
    public async Task ShouldGetToDoById()
    {
        // Arrange
        // Act
        var response = await this._toDoApi.GetTodoById(this._httpRequest.Object, this._idFixture.Id, this._logger.Object) as OkObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status200OK);

        var todo = response.Value as ToDoModel;
        todo.ShouldNotBeNull();
        todo.Id.ShouldBe(this._idFixture.Id);
        todo.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    [OrderTestByAscendingNumber(4)]
    public async Task ShouldMarkToDoAsDone()
    {
        // Arrange
        // Act
        var response = await this._toDoApi.MarkToDoAsDone(this._httpRequest.Object, this._idFixture.Id, this._logger.Object) as NoContentResult;
        
        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    [OrderTestByAscendingNumber(5)]
    public async Task ShouldReturnNotFoundForNonExistentToDo()
    {
        // Arrange
        string id = Guid.NewGuid().ToString("n");
        
        // Act
        var response = await this._toDoApi.MarkToDoAsDone(this._httpRequest.Object, id, this._logger.Object) as NotFoundObjectResult;
        
        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    [OrderTestByAscendingNumber(6)]
    public async Task ShouldReturnNotFoundWhenUpdateToDoIsCalled()
    {
        // Arrange
        // Act
        var response = await this._toDoApi.UpdateToDo(this._httpRequest.Object, Guid.NewGuid().ToString("n"), this._logger.Object) as NotFoundObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
    
    [Fact]
    [OrderTestByAscendingNumber(7)]
    public async Task ShouldReturnNoContentWhenToDoIsUpdated()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("n");
        var toDo = new ToDoModel { Id = id, TaskDescription = "Dusting", CreatedAt = DateTime.Now };
        ToDoFunctionApp.ToDoApi.ToDos = new List<ToDoModel> { toDo };

        toDo.TaskDescription = "Dusting and washing up";

        var body = await CreateStreamForHttpRequest(toDo);
        this._httpRequest.SetupGet(x => x.Body).Returns(body);

        // Act
        var response = await this._toDoApi.UpdateToDo(this._httpRequest.Object, id, this._logger.Object) as NoContentResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);        
    }

    [Fact]
    [OrderTestByAscendingNumber(8)]
    public async Task ShouldReturnNotFoundWhenDeleteToDoIsCalled()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("n");

        // Act
        var response = await this._toDoApi.DeleteToDo(this._httpRequest.Object, id, this._logger.Object) as NotFoundObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    [OrderTestByAscendingNumber(9)]
    public async Task ShouldReturnNoContentWhenToDoIsDeleted()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("n");
        var todo = new ToDoModel { Id = id, CreatedAt = DateTime.Now, TaskDescription = "De-clutter bookshelf" };
        ToDoApi.ToDos = new List<ToDoModel> { todo };
        
        // Act
        var response = await this._toDoApi.DeleteToDo(this._httpRequest.Object, id, this._logger.Object) as NoContentResult;
        
        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }
    */
}