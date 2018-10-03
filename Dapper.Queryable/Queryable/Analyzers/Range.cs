using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public class Range : Where
    {
        protected static readonly string Suffix = nameof(Range);

        protected override bool IsSatisfied(Context context, PropertyInfo queryProperty)
        {
            if (!queryProperty.Name.EndsWith(Suffix))
            {
                return false;
            }

            string targetModelPropertyName = queryProperty.Name.Substring(0, queryProperty.Name.Length - Suffix.Length);
            Type propertyType1 = queryProperty.PropertyType;
            if (propertyType1.IsGenericType && propertyType1.GetGenericTypeDefinition() == typeof(Range<>))
            {
                Type type = propertyType1.GetGenericArguments().First();
                if (!(type == typeof(int)) && !(type == typeof(Decimal)) && !(type == typeof(DateTime)))
                    return false;
                PropertyInfo propertyInfo =
                    context.ModelProperties.FirstOrDefault(x => x.Name == targetModelPropertyName);
                if (propertyInfo == null)
                    return false;
                Type propertyType2 = propertyInfo.PropertyType;
                if (propertyType2 == type)
                    return true;
                if (propertyType2.IsGenericType && propertyType2.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return propertyType2.GetGenericArguments().First() == type;
            }
            return false;
        }

        protected override Expression GetWhereClause(Context context, PropertyInfo queryProperty)
        {
            string targetModelPropertyName = queryProperty.Name.Substring(0, queryProperty.Name.Length - Suffix.Length);
            var type = context.ModelProperties.FirstOrDefault(x => x.Name == targetModelPropertyName)?.PropertyType;
            if (type == null)
            {
                throw new ArgumentException("ModelProperties Not Exist Range Name ");
            }

            if (type.IsGenericType)
            {
                type = type.GetGenericArguments().First();
            }

            var columnName = this.GetColumnName(context.ModelType, targetModelPropertyName);

            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(context.Analyzer);
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;

            var type2 = typeof(Range<>).MakeGenericType(type);
            var returnType = typeof(Nullable<>).MakeGenericType(type);
            var hasValueProperty = returnType.GetProperty("HasValue") ?? throw new InvalidOperationException();
            var valueProperty = returnType.GetProperty("Value") ?? throw new InvalidOperationException();

            var rangeExpr = Expression.Property(context.QueryExpression, queryProperty);
            var leftExpr = Expression.Property(rangeExpr,
                type2.GetProperty("Left", returnType) ?? throw new InvalidOperationException());
            var leftExclusiveExpre = Expression.Property(rangeExpr,
                type2.GetProperty("LeftExclusive") ?? throw new InvalidOperationException());
            var rightExpr = Expression.Property(rangeExpr,
                type2.GetProperty("Right", returnType) ?? throw new InvalidOperationException());
            var rightExclusiveExpr = Expression.Property(rangeExpr,
                type2.GetProperty("RightExclusive") ?? throw new InvalidOperationException());

            var expressionList = new List<Expression>();

            var stringParam1 = this.ConcatExpression(
                Expression.Condition(leftExclusiveExpre,
                    Expression.Constant($" AND {startDelimiter}{columnName}{endDelimiter} > "),
                    Expression.Constant($" AND {startDelimiter}{columnName}{endDelimiter} >= ")),
                Expression.Constant($"@{columnName}Left")
            );

            var leftBodyExpr = Expression.Block(this.CallStringBuilderAppend(context.SqlExpression, stringParam1),
                this.CallAddParameters(context.ParametersExpression, Expression.Constant($"@{columnName}Left"),
                    Expression.Property(leftExpr, valueProperty)));

            expressionList.Add(Expression.IfThen(
                Expression.AndAlso(
                    Expression.NotEqual(leftExpr,
                        Expression.Constant(null)),
                    Expression.Property(leftExpr, hasValueProperty)), leftBodyExpr
            ));

            var stringParam2 = this.ConcatExpression(
                Expression.Condition(rightExclusiveExpr,
                    Expression.Constant($" AND {startDelimiter}{columnName}{endDelimiter} < "),
                    Expression.Constant($" AND {startDelimiter}{columnName}{endDelimiter} <= ")),
                Expression.Constant($"@{columnName}Right"));

            var rightBodyExpr = Expression.Block(this.CallStringBuilderAppend(context.SqlExpression, stringParam2),
                this.CallAddParameters(context.ParametersExpression, Expression.Constant($"@{columnName}Right"),
                    Expression.Property(rightExpr, valueProperty)));

            expressionList.Add(Expression.IfThen(
                Expression.AndAlso(
                    Expression.NotEqual(rightExpr,
                        Expression.Constant(null)),
                    Expression.Property(rightExpr, hasValueProperty)), rightBodyExpr
            ));

            return Expression.IfThen(
                Expression.NotEqual(rangeExpr,
                    Expression.Constant(null)),
                Expression.Block(expressionList));
        }
    }
}
