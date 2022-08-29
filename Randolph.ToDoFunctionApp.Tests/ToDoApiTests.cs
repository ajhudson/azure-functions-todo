using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Randolph.ToDoFunctionApp.Models;
using Randolph.ToDoFunctionApp.Tests.CustomAttributes;
using Shouldly;
using Xunit.Abstractions;

namespace Randolph.ToDoFunctionApp.Tests;

[TestCaseOrderer("Randolph.ToDoFunctionApp.Tests.UnitTestOrderers.AscendingNumberOrderer", "Randolph.ToDoFunctionApp.Tests")]
public class ToDoApi : TestsBase, IClassFixture<StackFixture>
{
    private readonly ITestOutputHelper _output;

    private readonly StackFixture _stackFixture;

    public ToDoApi(ITestOutputHelper output, StackFixture stackFixture)
    {
        this._output = output;
        this._stackFixture = stackFixture;
    }

    [Fact]
    [OrderTestByAscendingNumber(1)]
    public async Task ShouldAddTodo()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var httpReq = new Mock<HttpRequest>();
        var model = new CreateToDoModel { TaskDescription = "Do something nice" };
        var body = await CreateStreamForHttpRequest(model);
        
        httpReq.Setup(x => x.Body).Returns(body);

        // Act
        var response = await ToDoFunctionApp.ToDoApi.CreateToDo(httpReq.Object, logger.Object) as CreatedAtRouteResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe((int)HttpStatusCode.Created);

        var responseModel = response.Value as ToDoModel;
        responseModel.ShouldNotBeNull();
        this._stackFixture.Stack.Push(responseModel.Id);
        this._output.WriteLine($"ToDo created with ID of {responseModel.Id}");
        await Task.Delay(500);
    }

    [Fact]
    [OrderTestByAscendingNumber(2)]
    public async Task ShouldGetAllToDos()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var httpReq = new Mock<HttpRequest>();

        // Act
        var response = await ToDoFunctionApp.ToDoApi.GetAllToDos(httpReq.Object, logger.Object) as OkObjectResult;

        // Assert
        response.ShouldNotBeNull();
        response.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        
        var allToDos = response.Value as List<ToDoModel>;
        allToDos.ShouldNotBeNull();
        var todo = allToDos.FirstOrDefault();
        todo.ShouldNotBeNull();

        var expectedId = this._stackFixture.Stack.Pop();
        todo.Id.ShouldBe(expectedId);

    }
}