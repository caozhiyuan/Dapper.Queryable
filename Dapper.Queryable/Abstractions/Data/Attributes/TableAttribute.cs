namespace Dapper.Queryable.Abstractions.Data.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public Analyzer Analyzer { get; set; }

        public string Db { get; set; }

        public string Name { get; set; }
    }
}

