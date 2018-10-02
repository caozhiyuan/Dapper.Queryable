# Dapper.Queryable


Queryable Analyzers

| Analyzer  |   Descprtion     |
| ------    |    ------        |
| Contains  | Id in @Ids       |
| Equal     | Id = @Ids        |
| OrderBy   | Order By Id Desc |
| Range     | Id>10 AND Id<0   |
| StringContains | Name like @NamePattern |


use this query (that can be transport)
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

