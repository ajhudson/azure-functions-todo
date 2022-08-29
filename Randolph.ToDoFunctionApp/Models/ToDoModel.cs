using System;

namespace Randolph.ToDoFunctionApp.Models;

public class ToDoModel : ToDoModelBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; }
}