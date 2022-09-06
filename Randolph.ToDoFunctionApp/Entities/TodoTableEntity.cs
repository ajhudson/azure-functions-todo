using System;

namespace Randolph.ToDoFunctionApp.Entities;

public class TodoTableEntity : BaseTableEntity
{
    public DateTime CreatedDt { get; set; }

    public string TaskDescription { get; set; }

    public bool IsCompleted { get; set; }
}