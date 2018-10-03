using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Dapper.Queryable.Configuration;
using Dapper.Queryable.Utils;

namespace Dapper.Queryable.CUD
{
    public class SqlOperationUpdate
    {
        private readonly Type _type;

        public SqlOperationUpdate(Type type)
        {
            this._type = type;
        }

        public Func<string> UpdateBuild()
        {
            var tableAttr = TableCache.GetTableDescriptor(_type);
            var cols = tableAttr.ColumnDescriptors;
            var options = tableAttr.Options;
            
            var dialectPatten = options.GetOperationPatten(SqlOperation.Update);

            var expressionList = new List<Expression>();
            var tableNameExpr = Expression.Variable(typeof(string), "tableName");
            var sqlpattenExpr = Expression.Variable(typeof(string), "sqlpatten");
            var columnsExpr = Expression.Variable(typeof(StringBuilder), "columns");
            var valuesExpr = Expression.Variable(typeof(StringBuilder), "values");
            var whereExpr = Expression.Variable(typeof(StringBuilder), "where");

            expressionList.Add(Expression.Assign(valuesExpr,
                Expression.New(typeof(StringBuilder))));

            expressionList.Add(Expression.Assign(whereExpr,
                Expression.New(typeof(StringBuilder))));

            expressionList.Add(Expression.Assign(columnsExpr,
                Expression.New(typeof(StringBuilder))));

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Constant(dialectPatten)));

            expressionList.Add(Expression.Assign(tableNameExpr, Expression.Constant(tableAttr.TableName)));

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Call(MethodInfoUtil.ReplaceMethod, new Expression[]
                {
                    sqlpattenExpr,
                    Expression.Constant("{TableName}"),
                    tableNameExpr
                })));
            
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;

            foreach (var col in cols)
            {
                if (!col.AutoIncrement)
                {
                    expressionList.Add(Expression.Call(columnsExpr,
                        MethodInfoUtil.StringBuilderAppend,
                        Expression.Constant($"{startDelimiter}{col.DbName}{endDelimiter} = @{col.Name},")));
                }

                if (col.IsPrimaryKey)
                {
                    expressionList.Add(Expression.Call(whereExpr,
                        MethodInfoUtil.StringBuilderAppend,
                        Expression.Constant($" WHERE {startDelimiter}{col.DbName}{endDelimiter} = @{col.Name}")));
                }
            }

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Call(MethodInfoUtil.ReplaceMethod, new Expression[]
                {
                    sqlpattenExpr,
                    Expression.Constant("{SetColumns}"),
                    Expression.Call(MethodInfoUtil.SubStringMethod, new Expression[]
                    {
                        Expression.Call(columnsExpr, MethodInfoUtil.StringBuilderToString)
                    })
                })));

            expressionList.Add(Expression.Assign(sqlpattenExpr,
                Expression.Call(MethodInfoUtil.ReplaceMethod, new Expression[]
                {
                    sqlpattenExpr,
                    Expression.Constant("{WhereClause}"),
                    Expression.Call(whereExpr, MethodInfoUtil.StringBuilderToString)
                })));

            var parameterExpressions = new[]
            {
                tableNameExpr,
                columnsExpr,
                valuesExpr,
                sqlpattenExpr,
                whereExpr
            };
            var block = Expression.Block(parameterExpressions, expressionList);

            return Expression.Lambda<Func<string>>(block).Compile();
        }
    }
}
