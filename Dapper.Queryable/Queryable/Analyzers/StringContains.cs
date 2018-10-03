using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public class StringContains : Where
    {
        protected static readonly string Suffix = "Pattern";

        protected override bool IsSatisfied(Context context, PropertyInfo queryProperty)
        {
            if (!(queryProperty.PropertyType == typeof(string)))
                return false;
            if (!queryProperty.Name.EndsWith(Suffix))
                return false;

            string targetModelPropertyName = queryProperty.Name.Substring(0, queryProperty.Name.Length - Suffix.Length);
            PropertyInfo propertyInfo = context.ModelProperties.FirstOrDefault(x => x.Name == targetModelPropertyName);

            return !(propertyInfo == null) && propertyInfo.PropertyType == queryProperty.PropertyType;
        }

        protected override Expression GetWhereClause(Context context, PropertyInfo queryProperty)
        {
            string str = queryProperty.Name.Substring(0, queryProperty.Name.Length - Suffix.Length);
            var memberExpression = Expression.Property(context.QueryExpression, queryProperty);

            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(context.Analyzer);
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;
            
            Expression keyExpr = Expression.Constant($"@{queryProperty.Name}");
            Expression stringParam = this.ConcatExpression(
                Expression.Constant($" AND {startDelimiter}{str}{endDelimiter} like "),
                keyExpr);

            var likeExpr = ConcatExpression(Expression.Constant("%"), memberExpression, Expression.Constant("%"));
            var bodyExpr = Expression.Block(
                this.CallStringBuilderAppend(context.SqlExpression, stringParam),
                this.CallAddParameters(context.ParametersExpression, keyExpr, likeExpr)
            );

            return Expression.IfThen(
                Expression.NotEqual(memberExpression, Expression.Constant(null)),
                bodyExpr);
        }
    }
}
;