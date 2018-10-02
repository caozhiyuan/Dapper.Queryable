using System;

namespace Dapper.Queryable.Abstractions.Cache
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoCacheAttribute : Attribute
    {
        public bool BackEnd { get; set; } = false;

        public long TimeMesc { get; set; }
    }
}

