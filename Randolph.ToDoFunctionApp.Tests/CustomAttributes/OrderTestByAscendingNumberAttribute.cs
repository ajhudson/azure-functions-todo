namespace Randolph.ToDoFunctionApp.Tests.CustomAttributes;

public class OrderTestByAscendingNumberAttribute : Attribute
{
    public int Id { get; set; }

    public OrderTestByAscendingNumberAttribute(int id)
    {
        this.Id = id;
    }
}