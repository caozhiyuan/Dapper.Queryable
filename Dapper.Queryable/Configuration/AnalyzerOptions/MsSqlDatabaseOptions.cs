namespace Dapper.Queryable.Configuration.AnalyzerOptions
{
    internal class MsSqlDatabaseOptions:SqlDatabaseOptions
    {
        public MsSqlDatabaseOptions()
        {
            this.StartDelimiter = "[";
            this.EndDelimiter = "]";
        }

        public override string GetOperationPatten(SqlOperation operation)
        {
            switch (operation)
            {
                case SqlOperation.Insert:
                    return
                        "INSERT INTO [{TableName}] ({Columns}) VALUES ({Values}); select SCOPE_IDENTITY() AS [Id] ";
                case SqlOperation.Update:
                    return "UPDATE [{TableName}] SET {SetColumns} {WhereClause}";
                case SqlOperation.Delete:
                    return "DELETE FROM [{TableName}] {WhereClause}";
                default:
                    return (string)null;
            }
        }

        public override string GetSelectSql(string columns, string tableName)
        {
            var str = $"SELECT {columns} FROM [{tableName}] with(nolock)";
            return str;
        }
        public override string GetPageSql(int skip, int take)
        {
            var pagesql = $" offset {skip} rows fetch next {take} rows only";
            return pagesql;
        }
    }
}
