using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public class Equal : Where
    {
        private static Type[] types = new Type[7]
        {
            typeof(int),
            typeof(Decimal),
            typeof(string),
            typeof(DateTime),
            typeof(bool),
            typeof(long),
            typeof(Guid)
        };

        protected override bool IsSatisfied(Context context, PropertyInfo queryProperty)
        {
            Type propertyType1 = queryProperty.PropertyType;
            PropertyInfo propertyInfo = (context.ModelProperties).FirstOrDefault((x => x.Name == queryProperty.Name));
            if (propertyInfo == null)
                return false;
            Type propertyType2 = propertyInfo.PropertyType;
            if (types.Contains(propertyType1))
            {
                if (propertyType2.IsGenericType && propertyType2.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return propertyType1 == propertyType2.GetGenericArguments().First();
                return propertyType1 == propertyInfo.PropertyType;
            }

            if (!propertyType1.IsGenericType || !(propertyType1.GetGenericTypeDefinition() == typeof(Nullable<>)))
                return false;
            Type type = propertyType1.GetGenericArguments().First();
            if (!types.Contains(type))
                return false;
            if (propertyType2.IsGenericType && propertyType2.GetGenericTypeDefinition() == typeof(Nullable<>))
                return type == ( propertyType2.GetGenericArguments()).First<Type>();
            return type == propertyType2;
        }

        protected override Expression GetWhereClause(Context context, PropertyInfo queryProperty)
        {
            var targetModelPropertyName = queryProperty.Name;
            var columnName = this.GetColumnName(context.ModelType, targetModelPropertyName);
            var propertyType = queryProperty.PropertyType;
            var memberExpression = Expression.Property(context.QueryExpression, queryProperty);

            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(context.Analyzer);
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;
            
            var constantExpression = Expression.Constant($" AND {startDelimiter}{columnName}{endDelimiter} = ");

            Expression keyExpr = Expression.Constant($"@{columnName}");
            Expression stringParam = this.ConcatExpression(constantExpression, keyExpr);

            var bodyExpr = Expression.Block(
                this.CallStringBuilderAppend(context.SqlExpression, stringParam),
                this.CallAddParameters(context.ParametersExpression, keyExpr, memberExpression)
            );

            if (propertyType == typeof(string) || propertyType.IsGenericType)
            {
                return Expression.IfThen(Expression.NotEqual(memberExpression, Expression.Constant(null)), bodyExpr);
            }

            return bodyExpr;
        }
    }
}
