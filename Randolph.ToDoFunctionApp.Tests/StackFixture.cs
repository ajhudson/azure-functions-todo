namespace Randolph.ToDoFunctionApp.Tests;

public class StackFixture : IDisposable
{
    private readonly Stack<string> _stack;

    public Stack<string> Stack => this._stack;

    public StackFixture()
    {
        this._stack = new Stack<string>();
    }

    public void Dispose()
    {
        this._stack.Clear();
    }
}