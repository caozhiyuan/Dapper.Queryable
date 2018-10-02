using System;
using System.Collections.Concurrent;
using Dapper.Queryable.Abstractions.Data;

namespace Dapper.Queryable.Queryable
{
    public class QueryHandlers
    {
        private static readonly ConcurrentDictionary<Type, Delegate> Handlers =
            new ConcurrentDictionary<Type, Delegate>();

        private static Compiler _compiler;

        private static Compiler Compiler
        {
            get { return _compiler ?? (_compiler = new Compiler()); }
        }

        public static Func<IQuery<TModel>, Clause> TryGet<TModel>(Type typeOfQuery)
        {
            if (!Handlers.TryGetValue(typeOfQuery, out var @delegate))
            {
                @delegate = Compiler.Compile<TModel>(typeOfQuery);
                Handlers.TryAdd(typeOfQuery, @delegate);
            }
            return (Func<IQuery<TModel>, Clause>) @delegate;
        }
    }
}
