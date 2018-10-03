# Dapper.Queryable


Queryable Analyzers

| Analyzer  |   Descprtion     |
| ------    |    ------        |
| Contains  | Id in @Ids       |
| Equal     | Id = @Ids        |
| OrderBy   | Order By Id Desc |
| Range     | Id>10 AND Id<0   |
| StringContains | Name like @NamePattern |


Table Application 
```C#
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
```

use this query (can be transport in Remote Procedure Call )
```C#
    var query = new ApplicationQuery()
    {
        Ids = new[] { 1, 2, 3 },
        IdRange = new Range<int>
        {
            Left = 1,
            LeftExclusive = true
        },
        NamePattern = "XX"
    };
```
buil sql

```sql
SELECT  [Id] As [Id] , [Name] As [name] , [CreateTime] As [CreateTime]  FROM [Application] with(nolock)   WHERE  [Id] IN @Ids AND [Name] like @NamePattern AND [Id] > @IdLeft
```

Detail In Test : https://github.com/caozhiyuan/Dapper.Queryable/blob/master/Dapper.Queryable.Test/DapperExtensionTest.cs

