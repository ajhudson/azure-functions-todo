using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Azure.Data.Tables;

namespace Randolph.ToDoFunctionApp.Extensions;

public static class EnumerableExtensions
{
    public static async IAsyncEnumerable<TModel> QueryAllToModelAsync<T, TModel>(
        this TableClient tableClient, 
        Func<T, TModel> mapperFunc,
        Expression<Func<T, bool>> query = null) where T : class, ITableEntity, new()
    {
        var queryResults = tableClient.QueryAsync(query);

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