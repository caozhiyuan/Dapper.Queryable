using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public class OrderBy : AbstractAnalyzer
    {
        protected static string BuildSort<T>(IQuery<T> query)
        {
            var analyzer = TableCache.GetDialect(typeof(T));
            var sb = new StringBuilder();
            if (query.OrderBys != null)
            {
                var len = query.OrderBys.Count;
                if (len > 0)
                {
                    sb.Append(" ORDER BY");
                }

                var cols = TableCache.GetColumnDescriptors(typeof(T));
                for (var index = 0; index < len; index++)
                {
                    var sort = query.OrderBys[index];
                    if (cols.All(n => n.DbName != sort.OrderField))
                    {
                        throw new ArgumentException("query.OrderBys OrderField Not Exist");
                    }

                    var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(analyzer);
                    var startDelimiter = options.StartDelimiter;
                    var endDelimiter = options.EndDelimiter;

                    sb.Append($" {startDelimiter}{sort.OrderField}{endDelimiter} {sort.OrderDirection}");
                    if (index != len - 1)
                    {
                        sb.Append(",");
                    }
                }

                return sb.ToString();
            }

            return string.Empty;
        }

        protected override void Analyze(Context context)
        {
            var method0 = typeof(OrderBy).GetMethod("BuildSort", BindingFlags.NonPublic | BindingFlags.Static);
            var method = method0?.MakeGenericMethod(context.ModelType);
            if (method == null)
            {
                throw new ArgumentException();
            }

            var bodyExpr = Expression.Assign(context.OrderExpression,
                Expression.Call(null, method, context.QueryExpression));
            context.Statements.Add(bodyExpr);
        }
    }
}
