using System.Linq.Expressions;
using AutoMapper;
using Azure;
using Azure.Data.Tables;
using Moq;
using Randolph.ToDoFunctionApp.Entities;
using Randolph.ToDoFunctionApp.Extensions;
using Randolph.ToDoFunctionApp.MappingProfiles;
using Randolph.ToDoFunctionApp.Models;
using Shouldly;

namespace Randolph.ToDoFunctionApp.Tests.Extensions;

public class EnumerableExtensionTests
{
    [Fact]
    public async Task ShouldMapEntitiesToModels()
    {
        // Arrange
        var tableClientMock = new Mock<TableClient>();
        
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ToDoMappingProfile>();
        });

        var mapper = mapperConfig.CreateMapper();
        
        var pages = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>>
        {
            Page<TodoTableEntity>.FromValues(new [] { CreateEntity("Task 1"), CreateEntity("Task 2") }, null, Mock.Of<Response>())
        });

        tableClientMock.Setup(x => x.QueryAsync<TodoTableEntity>(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(pages);

        // Act
        var results = tableClientMock.Object.QueryAllToModelAsync<TodoTableEntity, ToDoModel>(entity => mapper.Map<TodoTableEntity, ToDoModel>(entity));
        
        // Assert
        var listOfResults = await results.ToListAsync();
        listOfResults.ShouldNotBeNull();
        listOfResults.Count.ShouldBe(2);
        listOfResults[0].TaskDescription.ShouldBe("Task 1");
        listOfResults[1].TaskDescription.ShouldBe("Task 2");
    }

    private static TodoTableEntity CreateEntity(string taskDescription) => new()
    {
        PartitionKey = Constants.PartitionKey,
        CreatedDt = DateTime.UtcNow,
        IsCompleted = false,
        TaskDescription = taskDescription
    };
}