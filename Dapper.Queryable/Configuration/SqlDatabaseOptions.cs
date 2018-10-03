using System;

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

        public virtual string GetOperationPatten(SqlOperation operation)
        {
            throw new NotImplementedException();
        }

        public virtual string GetSelectSql(string columns, string tableName)
        {
            throw new NotImplementedException();
        }

        public virtual string GetPageSql(int skip, int take)
        {
            throw new NotImplementedException();
        }
    }
}
