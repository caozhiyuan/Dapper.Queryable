﻿using Dapper.Queryable.Utils;

namespace Dapper.Queryable.Configuration.AnalyzerOptions
{
    internal class MySqlDatabaseOptions : SqlDatabaseOptions
    {
        public MySqlDatabaseOptions()
        {
            this.StartDelimiter = this.EndDelimiter = "`";
        }

        public override string GetOperationPatten(SqlOperation operation)
        {
            switch (operation)
            {
                case SqlOperation.Insert:
                    return "INSERT INTO `{TableName}` ({Columns}) VALUES ({Values}); SELECT @@IDENTITY AS `Id` ";
                case SqlOperation.Update:
                    return "UPDATE `{TableName}` SET {SetColumns} {WhereClause}";
                case SqlOperation.Delete:
                    return "DELETE FROM `{TableName}` {WhereClause}";
                default:
                    return null;
            }
        }

        public override string GetSelectSql(string columns, string tableName)
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append("SELECT ");
            sb.Append(columns);
            sb.Append(" FROM `");
            sb.Append(tableName);
            sb.Append("`");
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override string GetPageSql(int skip, int take)
        {
            var pagesql = $" limit {skip},{take}";
            return pagesql;
        }
    }
}
