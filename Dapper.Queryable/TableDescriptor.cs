using System;
using System.Collections.Generic;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable
{
    public class TableDescriptor
    {
        public Analyzer Analyzer { get; set; }

        public string Db { get; set; }

        public string TableName { get; set; }

        public List<ColumnDescriptor> ColumnDescriptors { get; set; }

        public SqlDatabaseOptions Options { get; set; }
    }

    public class ColumnDescriptor
    {
        public string Name { get; set; }

        public string DbName { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool AutoIncrement { get; set; }

        public Action<object, object> SetPrimaryKey { get; set; }
    }
}
