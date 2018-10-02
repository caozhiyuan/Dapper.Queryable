using System;

namespace Dapper.Queryable
{
    public class ColumnDescriptor
    {
        public string Name { get; set; }

        public string DbName { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool AutoIncrement { get; set; }

        public Action<object, object> SetPrimaryKey { get; set; }
    }
}
