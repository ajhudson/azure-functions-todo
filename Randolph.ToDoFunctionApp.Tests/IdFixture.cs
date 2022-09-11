namespace Randolph.ToDoFunctionApp.Tests;

public class IdFixture : IDisposable
{
    public Guid? Id { get; set; }

    public void Dispose()
    {
        this.Id = null;
    }
}