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
                    return (string)null;
            }
        }

        public override string GetSelectSql(string columns, string tableName)
        {
            var str = $"SELECT {columns} FROM `{tableName}`";
            return str;
        }

        public override string GetPageSql(int skip, int take)
        {
            var pagesql = $" limit {skip},{take}";
            return pagesql;
        }
    }
}
