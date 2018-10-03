using System.Diagnostics;
using Xunit;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Queryable;
using Xunit.Abstractions;

namespace Dapper.Queryable.Test
{
    public class QueryableTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public QueryableTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void FromCacheQueryTest()
        {
            var query = new ApplicationQuery
            {
                Ids = new[] { 1, 2, 3 }
            };
            var flag = query.FromCache();
            Assert.True(flag);

            var query2 = new ApplicationQuery
            {
                Ids = new[] { 1, 2, 3 },
                IdRange = new Range<int>
                {
                    Left = 1,
                    LeftExclusive = true
                },
                NamePattern = "XX"
            };
            var flag2 = query2.FromCache();
            Assert.False(flag2);

            var query3 = new ApplicationQuery();
            var flag3 = query3.FromCache();
            Assert.False(flag3);
        }

        [Fact]
        public void ApplicationQueryTest()
        {
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
            var cause = new SqlBuilder().SelectAsync(query);
            Assert.Equal("SELECT  [Id] As [Id] , [Name] As [name] , [CreateTime] As [CreateTime]  FROM [Application] with(nolock) WHERE  [Id] IN @Ids AND [Name] like @NamePattern AND [Id] > @IdLeft", cause.Sql);
        }

        [Fact]
        public void ApplicationQueryTest2()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100000; i++)
            {
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
                new SqlBuilder().SelectAsync(query);
            }
            sw.Stop();
            _outputHelper.WriteLine($"ElapsedMilliseconds:{sw.ElapsedMilliseconds}");
        }

        [Fact]
        public void PageTest()
        {
            var query = new ApplicationQuery
            {
                Take = 10,
                Skip = 10
            };
            var cause = new SqlBuilder().SelectAsync(query);
            Assert.Equal("SELECT  [Id] As [Id] , [Name] As [name] , [CreateTime] As [CreateTime]  FROM [Application] with(nolock) ORDER BY [Id] DESC offset 10 rows fetch next 10 rows only ;SELECT COUNT(1) from [Application] ;", cause.Sql);
        }

        [Fact]
        public void InsertBuilderTest()
        {
            var sql = new SqlBuilder().Insert(typeof(Application));
            Assert.Equal(
                "INSERT INTO [Application] ([name],[CreateTime]) VALUES (@Name,@CreateTime); select SCOPE_IDENTITY() AS [Id] ",
                sql);
        }

        [Fact]
        public void InsertMutilBuilderTest()
        {
            var sql = new SqlBuilder().Insert(typeof(Application[]));
            Assert.Equal(
                "INSERT INTO [Application] ([name],[CreateTime]) VALUES (@Name,@CreateTime)",
                sql);
        }

        [Fact]
        public void UpdateBuilderTest()
        {
            var sql = new SqlBuilder().Update(typeof(Application));
            Assert.Equal("UPDATE [Application] SET [name] = @Name,[CreateTime] = @CreateTime  WHERE [Id] = @Id", sql);
        }

        [Fact]
        public void DeleteBuilderTest()
        {
            var sql = new SqlBuilder().Delete(typeof(Application));
            Assert.Equal("DELETE FROM [Application]  WHERE [Id] = @Id", sql);
        }
    }
}
