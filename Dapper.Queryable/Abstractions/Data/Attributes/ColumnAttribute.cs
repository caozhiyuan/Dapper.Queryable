namespace Dapper.Queryable.Abstractions.Data.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; set; }
    }
}

