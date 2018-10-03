using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Utils;

namespace Dapper.Queryable.Queryable.Analyzers
{
    public abstract class AbstractAnalyzer : IAnalyzer
    {
        protected abstract void Analyze(Context context);

        void IAnalyzer.Analyze(Context context)
        {
            this.Analyze(context);
        }

        protected Expression ConcatExpression(Expression left, Expression right)
        {
            var methodInfos = MethodInfoUtil.StringConcatMethodInfos.Value;
            return Expression.Call(null, methodInfos.First(m => m.GetParameters().Length == 2), left, right);
        }

        protected Expression ConcatExpression(Expression arg0, Expression arg1, Expression arg2)
        {
            var methodInfos = MethodInfoUtil.StringConcatMethodInfos.Value;
            return Expression.Call(null, methodInfos.First(m => m.GetParameters().Length == 3), arg0, arg1, arg2);
        }

        protected Expression CallAddParameters(Expression parametersExpr, Expression keyExpr, Expression valueExpr)
        {
            MethodInfo method = MethodInfoUtil.DynanicParametersAddMethod;
            return Expression.Call(parametersExpr, method, keyExpr, Expression.Convert(valueExpr, typeof(object)));
        }

        protected Expression CallStringBuilderAppend(Expression stringBuilderInst, Expression stringParam)
        {
            return Expression.Call(stringBuilderInst, MethodInfoUtil.StringBuilderAppend, stringParam);
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
