using System;

namespace Randolph.ToDoFunctionApp.Models;

public class ToDoModel : ToDoModelBase
{
    public Guid ToDoId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; }
}