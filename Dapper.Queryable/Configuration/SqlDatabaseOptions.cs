namespace Dapper.Queryable.Configuration
{
    public class SqlDatabaseOptions
    {
        public SqlDatabaseOptions()
        {
            this.StartDelimiter = this.EndDelimiter = "\"";
        }

        public string StartDelimiter { get; protected set; }

        public string EndDelimiter { get; protected set; }
    }
}
