using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Abstractions.Data.Attributes;

namespace Dapper.Queryable.Queryable
{
    public class Compiler
    {
        public Func<IQuery<TModel>, Clause> Compile<TModel>(Type typeOfQuery)
        {
            var modelType = typeof(TModel);

            var clauseExpr = Expression.Variable(typeof(Clause), "clause");
            var whereExpr = Expression.Variable(typeof(StringBuilder), "where");
            var orderExpr = Expression.Variable(typeof(string), "order");
            var parametersExpr = Expression.Variable(typeof(DynanicParameters), "parameters");

            var concreteQueryExpr = Expression.Variable(typeOfQuery, "concreteQuery");
            var queryParameterExpr = Expression.Parameter(typeof(IQuery<TModel>), "query");

            IList<Expression> expressionList = new List<Expression>();

            expressionList.Add(Expression.Assign(concreteQueryExpr,
                Expression.Convert(queryParameterExpr, typeOfQuery)));

            expressionList.Add(Expression.Assign(clauseExpr,
                Expression.New(typeof(Clause))));

            expressionList.Add(Expression.Assign(parametersExpr,
                Expression.New(typeof(DynanicParameters))));

            expressionList.Add(Expression.Assign(whereExpr,
                Expression.New(typeof(StringBuilder))));

            expressionList.Add(Expression.Assign(orderExpr,
                Expression.Constant(string.Empty)));

            var analyzerArray = CreateAnalyzer(modelType);
            if (analyzerArray != null && analyzerArray.Any())
            {
                var context = new Context(whereExpr, parametersExpr, orderExpr, concreteQueryExpr, modelType, typeOfQuery);
                foreach (IAnalyzer analyzer in analyzerArray)
                {
                    analyzer.Analyze(context);
                }

                var statements = context.Statements;
                if (statements.Any())
                {
                    expressionList.Add(Expression.Block(statements));
                }
            }

            var target = Expression.Label(typeof(Clause));

            //clause.Where = where;
            var whereProperty = typeof(Clause).GetProperty("Where") ?? throw new InvalidOperationException();
            var assignExprWhere = Expression.Assign(Expression.Property(clauseExpr, whereProperty),
                Expression.Call(null, GetWhereMethodInfo, whereExpr));

            expressionList.Add(assignExprWhere);

            //clause.OrderBy = order;
            var orderProperty = typeof(Clause).GetProperty("OrderBy") ?? throw new InvalidOperationException();
            var assignExprorder = Expression.Assign(Expression.Property(clauseExpr, orderProperty),
                orderExpr);
            expressionList.Add(assignExprorder);

            //clause.Parameters = parameters;
            var parametersProperty = typeof(Clause).GetProperty("Parameters") ?? throw new InvalidOperationException();
            var assignExprParameters = Expression.Assign(Expression.Property(clauseExpr, parametersProperty), parametersExpr);
            expressionList.Add(assignExprParameters);

            var gotoExpression = Expression.Return(target, clauseExpr);
            var labelExpression = Expression.Label(target, Expression.Default(typeof(Clause)));
            expressionList.Add(gotoExpression);
            expressionList.Add(labelExpression);

            var parameterExpressions = new[]
            {
                clauseExpr,
                parametersExpr,
                whereExpr,
                orderExpr,
                concreteQueryExpr
            };
            var block = Expression.Block(parameterExpressions, expressionList);

            return Expression.Lambda<Func<IQuery<TModel>, Clause>>(block, queryParameterExpr).Compile();
        }

        private static IAnalyzer[] CreateAnalyzer(Type typeOfModel)
        {
            var tableAttribute = SqlBuilderUtil.GetTable(typeOfModel);
            switch (tableAttribute.Analyzer)
            {
                case Analyzer.Ms:
                    return MsAnalyzer.Create();
                case Analyzer.My:
                    return MyAnalyzer.Create();
                default:
                    throw new ArgumentException(nameof(Analyzer));
            }
        }

        private static readonly MethodInfo GetWhereMethodInfo =
            typeof(Compiler).GetMethod("GetWhere", BindingFlags.NonPublic | BindingFlags.Static);

        private static string GetWhere(StringBuilder sb)
        {
            var str = sb.ToString();
            str = str.Trim();
            if (str.StartsWith("AND"))
            {
                str = str.Substring(3);
            }
            return str.Length > 0 ? $" WHERE {str}" : string.Empty;
        }
    }
}