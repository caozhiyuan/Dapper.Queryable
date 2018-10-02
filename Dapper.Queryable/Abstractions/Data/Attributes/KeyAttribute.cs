namespace Dapper.Queryable.Abstractions.Data.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class KeyAttribute : Attribute
    {
        public bool AutoIncrement { get; set; }
    }
}

