using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Randolph.ToDoFunctionApp.Entities;

public class TodoTableEntity : TableEntity
{
    public DateTime CreateDt { get; set; }

    public string TaskDescription { get; set; }

    public bool IsCompleted { get; set; }
}