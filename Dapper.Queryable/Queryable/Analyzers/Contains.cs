using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public class Contains : Where
    {
        protected static readonly string Suffix = "s";

        protected override bool IsSatisfied(Context context, PropertyInfo queryProperty)
        {
            if (!queryProperty.Name.EndsWith(Suffix) || !queryProperty.PropertyType.IsArray)
                return false;

            Type elementType = queryProperty.PropertyType.GetElementType();
            if (elementType == typeof(int) || elementType == typeof(long) || elementType == typeof(string))
            {
                string targetModelPropertyName =
                    queryProperty.Name.Substring(0, queryProperty.Name.Length - Suffix.Length);
                PropertyInfo propertyInfo =
                    (context.ModelProperties).FirstOrDefault((x => x.Name == targetModelPropertyName));
                if (propertyInfo == null)
                    return false;
                if (propertyInfo.PropertyType == elementType)
                    return true;
                Type propertyType = propertyInfo.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return propertyType.GetGenericArguments().First() == elementType;
            }

            return false;
        }

        protected override Expression GetWhereClause(Context context, PropertyInfo queryProperty)
        {
            string propertyName = queryProperty.Name.Substring(0, queryProperty.Name.Length - Suffix.Length);
            string columnName = this.GetColumnName(context.ModelType, propertyName);

            var memberExpression = Expression.Property(context.QueryExpression, queryProperty);
            var keyExpr = Expression.Constant($"@{propertyName}{Suffix}");

            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(context.Analyzer);
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;
            
            var stringParam = this.ConcatExpression(
                Expression.Constant($" AND {startDelimiter}{columnName}{endDelimiter} IN "),
                keyExpr);

            var notNullExpr = Expression.NotEqual(memberExpression, Expression.Constant(null));
            var lenExpr = Expression.GreaterThan(Expression.ArrayLength(memberExpression), Expression.Constant(0));

            var bodyExpr = Expression.Block(
                this.CallStringBuilderAppend(context.SqlExpression, stringParam),
                this.CallAddParameters(context.ParametersExpression, keyExpr, memberExpression)
            );

            return Expression.Block(Expression.IfThen(
                Expression.AndAlso(notNullExpr, lenExpr),
                bodyExpr));
        }
    }
}
