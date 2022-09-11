using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using Randolph.ToDoFunctionApp.Entities;

namespace Randolph.ToDoFunctionApp.Tests;

public class DeleteCompletedTasksTimerFunctionTests
{
    private readonly Mock<TimerInfo> _timerInfoMock;

    private readonly Mock<TableClient> _tableClientMock;

    private readonly Mock<ILogger> _loggerMock;

    public DeleteCompletedTasksTimerFunctionTests()
    {
        var timerScheduleMock = new Mock<TimerSchedule>();
        var scheduleStatusMock = new Mock<ScheduleStatus>();
        this._timerInfoMock = new Mock<TimerInfo>(timerScheduleMock.Object, scheduleStatusMock.Object, false);
        
        this._tableClientMock = new Mock<TableClient>();
        this._loggerMock = new Mock<ILogger>();
    }
    
    [Fact]
    public async Task ShouldDeleteCompletedTasksWhenRun()
    {
        // Arrange
        var todos = new List<TodoTableEntity>
        {
            CreateEntity("Task 1", true),
            CreateEntity("Task 2", true),
            CreateEntity("Task 3", true)
        };

        var todoPages = Page<TodoTableEntity>.FromValues(todos.AsReadOnly(), null, Mock.Of<Response>());
        var todoPagable = AsyncPageable<TodoTableEntity>.FromPages(new List<Page<TodoTableEntity>> { todoPages });

        this._tableClientMock.Setup(x => x.QueryAsync(It.IsAny<Expression<Func<TodoTableEntity, bool>>>(),
                It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(todoPagable);
        
        this._tableClientMock.Setup(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ETag>(), It.IsAny<CancellationToken>())).Verifiable();

        // Act
        await DeleteCompletedTasksTimerFunction.RunAsync(this._timerInfoMock.Object, this._tableClientMock.Object, this._loggerMock.Object);

        // Assert
        this._tableClientMock.Verify(x => x.DeleteEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ETag>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    private static TodoTableEntity CreateEntity(string taskDescription, bool isCompleted)
    {
        return new TodoTableEntity
        {
            RowKey = Guid.NewGuid().ToString("n"),
            CreatedDt = DateTime.UtcNow,
            PartitionKey = Constants.PartitionKey,
            TaskDescription = taskDescription,
            IsCompleted = isCompleted
        };
    }
}