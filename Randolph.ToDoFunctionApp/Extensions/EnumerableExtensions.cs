using System;
using System.Collections.Generic;
using Azure.Data.Tables;

namespace Randolph.ToDoFunctionApp.Extensions;

public static class EnumerableExtensions
{
    public static async IAsyncEnumerable<TModel> QueryAllToModelAsync<T, TModel>(this TableClient tableClient, Func<T, TModel> mapperFunc) where T : class, ITableEntity, new()
    {
        var queryResults = tableClient.QueryAsync<T>();

        await foreach (var currentPage in queryResults.AsPages())
        {
            foreach (var currentValue in currentPage.Values)
            {
                var model = mapperFunc(currentValue);

                yield return model;
            }
        }
    }
}