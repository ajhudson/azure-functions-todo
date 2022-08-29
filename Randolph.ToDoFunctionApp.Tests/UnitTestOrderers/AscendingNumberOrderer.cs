using Randolph.ToDoFunctionApp.Tests.CustomAttributes;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Randolph.ToDoFunctionApp.Tests.UnitTestOrderers;

public class AscendingNumberOrderer : ITestCaseOrderer
{
    private readonly IMessageSink _messageSink;

    public AscendingNumberOrderer(IMessageSink messageSink)
    {
        this._messageSink = messageSink;
    }
    
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        var testMethodOrdering = new List<KeyValuePair<int, TTestCase>>();
        string attribFullyQualifiedName = typeof(OrderTestByAscendingNumberAttribute).AssemblyQualifiedName!;

        foreach (TTestCase testCase in testCases)
        {
            int? priority = testCase.TestMethod.Method.GetCustomAttributes(attribFullyQualifiedName)
                .FirstOrDefault()?.GetConstructorArguments().Cast<int>().SingleOrDefault();

            if (priority != null)
            {
                testMethodOrdering.Add(new KeyValuePair<int, TTestCase>(priority.Value, testCase));
            }
        }

        var orderedTests = testMethodOrdering.OrderBy(x => x.Key);

        foreach (var (key, testCase) in orderedTests)
        {
            this._messageSink.OnMessage(new DiagnosticMessage($"{key}: {testCase.TestMethod.Method.Name}"));
            yield return testCase;
        }
    }
}