namespace Randolph.ToDoFunctionApp.Tests;

public class IdFixture : IDisposable
{
    public string? Id { get; set; }

    public void Dispose()
    {
        this.Id = null;
    }
}