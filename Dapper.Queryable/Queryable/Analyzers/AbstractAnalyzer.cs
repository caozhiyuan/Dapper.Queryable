using Dapper.Queryable.Abstractions.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public abstract class AbstractAnalyzer : IAnalyzer
    {
        protected abstract void Analyze(Context context);

        void IAnalyzer.Analyze(Context context)
        {
            this.Analyze(context);
        }

        protected virtual Expression ConcatExpression(Expression left, Expression right)
        {
            return Expression.Call(null, typeof(string).GetMethods().First(m =>
            {
                if (m.Name == "Concat")
                    return m.GetParameters().Length == 2;
                return false;
            }), left, right);
        }

        protected virtual Expression ConcatExpression(Expression arg0, Expression arg1, Expression arg2)
        {
            return Expression.Call(null, typeof(string).GetMethods().First(m =>
            {
                if (m.Name == "Concat")
                    return m.GetParameters().Length == 3;
                return false;
            }), arg0, arg1, arg2);
        }

        protected virtual Expression CallAddParameters(Expression parametersExpr, Expression keyExpr, Expression valueExpr)
        {
            MethodInfo method = typeof(DynanicParameters).GetMethod("Add", new[]
            {
                typeof(string),
                typeof(object)
            }) ?? throw new InvalidOperationException();

            return Expression.Call(parametersExpr, method, keyExpr, Expression.Convert(valueExpr, typeof(object)));
        }

        protected virtual Expression CallStringBuilderAppend(Expression stringBuilderInst, Expression stringParam)
        {
            MethodInfo method = typeof(StringBuilder).GetMethod("Append", new Type[]
            {
                typeof(string)
            }) ?? throw new InvalidOperationException();
            return Expression.Call(stringBuilderInst, method, stringParam);
        }

        protected string GetColumnName(Type modelType, string propertyName)
        {
            string str = propertyName;
            PropertyInfo property = modelType.GetProperty(propertyName);
            if (property != null)
            {
                ColumnAttribute customAttribute =
                    property.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;
                if (customAttribute != null && customAttribute.Name != null)
                    str = customAttribute.Name;
            }

            return str;
        }
    }
}
