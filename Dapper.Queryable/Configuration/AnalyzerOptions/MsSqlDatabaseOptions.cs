namespace Dapper.Queryable.Configuration.AnalyzerOptions
{
    internal class MsSqlDatabaseOptions:SqlDatabaseOptions
    {
        public MsSqlDatabaseOptions()
        {
            this.StartDelimiter = "[";
            this.EndDelimiter = "]";
        }
    }
}
