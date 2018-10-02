using System;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Queryable;

namespace Dapper.Queryable
{
    public interface ISqlBuilder
    {
        string Insert(Type type);

        string Update(Type type);

        string Delete(Type type);

        Clause SelectAsync<TModel>(IQuery<TModel> query);
    }
}
