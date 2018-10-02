namespace Dapper.Queryable.Configuration.AnalyzerOptions
{
    internal class MySqlDatabaseOptions : SqlDatabaseOptions
    {
        public MySqlDatabaseOptions()
        {
            this.StartDelimiter = this.EndDelimiter = "`";
        }
    }
}
