using System;
using Dapper.Queryable.Abstractions.Cache;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Abstractions.Data.Attributes;

namespace Dapper.Queryable.Test
{

    [Table(Analyzer = Analyzer.Ms, Db = "systest", Name = "Application")]
    public class Application : IModel
    {
        [Key(AutoIncrement = true)]
        public int Id { get; set; }

        [Column(Name = "name")]
        public string Name { get; set; }

        public DateTime CreateTime { get; set; }
    }

    public class ApplicationQuery : AbstractQuery<Application>
    {
        [CacheKey]
        public int[] Ids { get; set; }

        public string NamePattern { get; set; }

        public string Name { get; set; }

        public Range<int> IdRange { get; set; }
    }

    [Table(Analyzer = Analyzer.My, Db = "systest", Name = "Application")]
    public class MyApplication : IModel
    {
        [Key(AutoIncrement = true)]
        public int Id { get; set; }

        [Column(Name = "name")]
        public string Name { get; set; }

        public DateTime CreateTime { get; set; }
    }

    public class MyApplicationQuery : AbstractQuery<MyApplication>
    {
        [CacheKey]
        public int[] Ids { get; set; }

        public string NamePattern { get; set; }

        public string Name { get; set; }

        public Range<int> IdRange { get; set; }
    }
}
