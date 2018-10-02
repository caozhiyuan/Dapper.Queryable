using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable.CUD
{
    public class SqlOperationInsert
    {
        private readonly Type _type;

        public SqlOperationInsert(Type type)
        {
            this._type = type;
        }

        public Func<string> InsertBuild()
        {
            var cols = SqlBuilderUtil.GetColumnDescriptors(_type);
            var tableAttr = SqlBuilderUtil.GetTable(_type);
            var dialectPatten = SqlBuilderUtil.GetDialectPatten(tableAttr.Analyzer, SqlOperation.Insert);


            var expressionList = new List<Expression>();
            var tableNameExpr = Expression.Variable(typeof(string), "tableName");
            var sqlpattenExpr = Expression.Variable(typeof(string), "sqlpatten");
            var columnsExpr = Expression.Variable(typeof(StringBuilder), "columns");
            var valuesExpr = Expression.Variable(typeof(StringBuilder), "values");

            expressionList.Add(Expression.Assign(valuesExpr,
                Expression.New(typeof(StringBuilder))));          

            expressionList.Add(Expression.Assign(columnsExpr,
                Expression.New(typeof(StringBuilder))));

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Constant(dialectPatten)));

            expressionList.Add(Expression.Assign(tableNameExpr, Expression.Constant(tableAttr.Name)));

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Call(MethodInfoUtil.ReplaceMethod, new Expression[]
                {
                    sqlpattenExpr,
                    Expression.Constant("{TableName}"),
                    tableNameExpr
                })));

            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(tableAttr.Analyzer);
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;

            foreach (var col in cols)
            {
                string columnName = col.DbName;
                if (!col.AutoIncrement)
                {
                    expressionList.Add(Expression.Call(columnsExpr,
                        MethodInfoUtil.StringBuilderAppend, Expression.Constant($"{startDelimiter}{columnName}{endDelimiter},")));

                    expressionList.Add(Expression.Call(valuesExpr,
                        MethodInfoUtil.StringBuilderAppend, Expression.Constant($"@{col.Name},")));
                }
            }

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Call(MethodInfoUtil.ReplaceMethod, new Expression[]
                {
                    sqlpattenExpr,
                    Expression.Constant("{Columns}"),
                    Expression.Call(MethodInfoUtil.SubStringMethod, new Expression[]
                    {
                        Expression.Call(columnsExpr, MethodInfoUtil.StringBuilderToString)
                    })
                })));

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Call(MethodInfoUtil.ReplaceMethod, new Expression[]
                {
                    sqlpattenExpr,
                    Expression.Constant("{Values}"),
                    Expression.Call(MethodInfoUtil.SubStringMethod, new Expression[]
                    {
                        Expression.Call(valuesExpr, MethodInfoUtil.StringBuilderToString)
                    })
                })));

            var parameterExpressions = new[]
            {
                tableNameExpr,
                columnsExpr,
                valuesExpr,
                sqlpattenExpr
            };
            var block = Expression.Block(parameterExpressions, expressionList);

            return Expression.Lambda<Func<string>>(block).Compile();
        }
    }
}
