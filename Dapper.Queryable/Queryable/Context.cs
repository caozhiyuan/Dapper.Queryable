using Dapper.Queryable.Abstractions.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.Queryable.Queryable
{
    public class Context
    {
        public Context(
            Expression sqlExpr,
            Expression parametersExpr,
            Expression orderExpr,
            Expression queryExpr,
            Type modelType, 
            Type queryType)
        {
            this.SqlExpression = sqlExpr;
            this.ParametersExpression = parametersExpr;
            this.OrderExpression = orderExpr;
            this.QueryExpression = queryExpr;
            this.ModelType = modelType;
            this.QueryType = queryType;

            this.ModelProperties = modelType.GetProperties()
                .Where(this.IsValidProperty)
                .ToArray();

            this.QueryProperties = queryType.GetProperties();

            this.Statements = new List<Expression>();

            this.Analyzer = TableCache.GetDialect(this.ModelType);
        }

        public Analyzer Analyzer { get; set; }

        public Expression ParametersExpression { get; set; }

        public Expression OrderExpression { get; set; }

        public Expression SqlExpression { get; set; }

        public Expression QueryExpression { get; set; }

        public Type ModelType { get; set; }

        public Type QueryType { get; set; }

        public PropertyInfo[] ModelProperties { get; set; }

        public PropertyInfo[] QueryProperties { get; set; }

        public IList<Expression> Statements { get; set; }

        private bool IsValidProperty(PropertyInfo property)
        {
            foreach (object customAttribute in property.GetCustomAttributes(false))
            {
                if (customAttribute is IgnoreAttribute)
                    return false;
            }
            return true;
        }
    }
}

