using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Queryable.Abstractions.Cache;
using Dapper.Queryable.Abstractions.Data;

namespace Dapper.Queryable.Queryable
{
    public static class QueryExtension
    {
        private static readonly ConcurrentDictionary<Type, Delegate> Handlers =
            new ConcurrentDictionary<Type, Delegate>();

        public static bool FromCache<TModel>(this IQuery<TModel> query) where TModel : new()
        {
            Type type = query.GetType();
            Func<IQuery<TModel>, bool> func;
            if (Handlers.ContainsKey(type))
            {
                Handlers.TryGetValue(type, out var @delegate);
                func = (Func<IQuery<TModel>, bool>) @delegate;
            }
            else
            {
                func = Gen<TModel>(type);
                Handlers.TryAdd(type, func);
            }

            if (func != null)
            {
                return func(query);
            }

            return false;
        }

        private static Func<IQuery<TModel>, bool> Gen<TModel>(Type typeOfQuery) where TModel : new()
        {
            var properties = typeOfQuery.GetProperties();
            var queryParamExpr = Expression.Parameter(typeof(IQuery<TModel>), "queryParam");
            var queryExpr = Expression.Variable(typeOfQuery, "query");
            var expressionList = new List<Expression>
            {
                Expression.Assign(queryExpr, Expression.Convert(queryParamExpr, typeOfQuery))
            };

            var target = Expression.Label(typeof(bool));
            foreach (var property in properties)
            {
                var customAttributes = property.GetCustomAttributes(false);
                if (!customAttributes.Any(attr => attr is CacheKeyAttribute))
                {
                    expressionList.Add(Expression.IfThen(
                        Expression.NotEqual(
                            Expression.Property(queryExpr, property),
                            Expression.Constant(null)),
                        Expression.Return(target, Expression.Constant(false))));
                }
                else
                {
                    expressionList.Add(Expression.IfThen(
                        Expression.Equal(
                            Expression.Property(queryExpr, property),
                            Expression.Constant(null)),
                        Expression.Return(target, Expression.Constant(false))));
                }
            }

            var gotoExpression = Expression.Return(target, Expression.Constant(true));
            var labelExpression = Expression.Label(target, Expression.Default(typeof(bool)));
            expressionList.Add(gotoExpression);
            expressionList.Add(labelExpression);

            var block = Expression.Block(new[]
            {
                queryExpr
            }, expressionList);

            return Expression.Lambda<Func<IQuery<TModel>, bool>>(block, queryParamExpr).Compile();
        }
    }
}
