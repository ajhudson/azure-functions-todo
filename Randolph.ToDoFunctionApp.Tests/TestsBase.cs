using System.Text.Json;

namespace Randolph.ToDoFunctionApp.Tests;

public abstract class TestsBase
{
    protected static async Task<Stream> CreateStreamForHttpRequest<T>(T model)
    {
        string serialisedModel = JsonSerializer.Serialize(model);
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        await writer.WriteAsync(serialisedModel);
        await writer.FlushAsync();
        ms.Position = 0;

        return ms;
    }
}