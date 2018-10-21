using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using MySql.Data.MySqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.Queryable.Test
{
    public class DbConnectionTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public DbConnectionTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        //private static string ConnectionString => $"User ID=root;Password=123456;Host=localhost;Port=3306;Database=test;Pooling=true;Min Pool Size=0;Max Pool Size=100;SslMode=None";
        private static string ConnectionString => $"Data Source=.;Initial Catalog=systest;Integrated Security=True";

        private async Task<IDbConnection> GetOpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        [Fact]
        public void RawQueryStressTest()
        {
            RawQueryTest();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            RawQueryTest();

            _outputHelper.WriteLine("ElapsedMilliseconds " + sw.ElapsedMilliseconds);
        }

        private void RawQueryTest()
        {
            const int c = 10000;
            CountdownEvent k = new CountdownEvent(c);
            Parallel.For(0, c, (i) =>
            {
                var task = RawGetApplicationById(c);
                task.ContinueWith(n =>
                {
                    if (n.IsFaulted)
                    {
                        _outputHelper.WriteLine($"{i} {n.Exception}");
                    }

                    k.Signal(1);
                });
            });
            k.Wait();
        }

        private async Task<Application> RawGetApplicationById(int id)
        {
            Application application;
            using (var connection = await GetOpenConnection())
            {
                application = await connection.QueryFirstOrDefaultAsync<Application>("SELECT  [Id] As [Id] , [Name] As [name] , [CreateTime] As [CreateTime]  FROM [Application] with(nolock) WHERE  [Id] IN @Ids", new {Ids = new[] {id}});
            }
            return application;
        }

        [Fact]
        public void QueryStressTest()
        {
            QueryTest();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            QueryTest();

            _outputHelper.WriteLine("ElapsedMilliseconds " + sw.ElapsedMilliseconds);
        }

        private void QueryTest()
        {
            const int c = 10000;
            CountdownEvent k = new CountdownEvent(c);
            Parallel.For(0, c, (i) =>
            {
                var task = GetApplicationById(c);
                task.ContinueWith(n =>
                {
                    if (n.IsFaulted)
                    {
                        _outputHelper.WriteLine($"{i} {n.Exception}");
                    }

                    k.Signal(1);
                });
            });
            k.Wait();
        }
        
        private async Task<Application> GetApplicationById(int id)
        {
            Application application;
            using (var connection = await GetOpenConnection())
            {
                application = await connection.SelectFirstOrDefaultAsync(new ApplicationQuery()
                {
                    Ids = new[] {id}
                });
            }
            return application;
        }

        [Fact]
        public async Task SelectPagedAsync()
        {
            using (var connection = await GetOpenConnection())
            {
                var app = new Application
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                };
                await connection.InsertAsync(app);

                var query = new ApplicationQuery()
                {
                    Take = 10,
                    Skip = 0,
                    NamePattern = app.Name
                };
                var applications = await connection.SelectAsync(query);
                Assert.NotEmpty(applications);
                Assert.Equal(1, query.Count);

                query = new ApplicationQuery()
                {
                    Take = 10,
                    Skip = 0,
                    NamePattern = "111111111111111111111111111111111111111111111"
                };
                applications = await connection.SelectAsync(query);
                Assert.Empty(applications);
                Assert.Equal(0, query.Count);

                applications = await connection.SelectAsync(new ApplicationQuery()
                {
                    Take = 10,
                    Skip = 0,
                    Name = app.Name
                });
                Assert.NotEmpty(applications);
            }
        }

        [Fact]
        public async Task UpdateAsync()
        {
            using (var connection = await GetOpenConnection())
            {
                var app = new Application
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                };
                await connection.InsertAsync(app);
                var application = await connection.SelectFirstOrDefaultAsync(new ApplicationQuery()
                {
                    Ids = new [] {app.Id}
                });
                if (application != null)
                {
                    application.Name = Guid.NewGuid().ToString();
                    await connection.UpdateAsync(application);
                }
            }
        }

        [Fact]
        public async Task DeleteAsync()
        {
            using (var connection = await GetOpenConnection())
            {
                var app = new Application
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                };
                await connection.InsertAsync(app);
                await connection.DeleteAsync(app);
            }
        }

        [Fact]
        public async Task InsertAsync()
        {
            using (var connection = await GetOpenConnection())
            {
                await connection.InsertAsync(new Application()
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                });
            }
        }

        [Fact]
        public async Task BatchInsertAsync()
        {
            await Assert.ThrowsAsync<SqlException>(async () =>
            {
                var transactionOptions = new TransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
                };
                using (var scope = new TransactionScope(TransactionScopeOption.Required,
                    transactionOptions,
                    TransactionScopeAsyncFlowOption.Enabled))
                {
                    var uniqueName = Guid.NewGuid().ToString();
                    using (var connection = await GetOpenConnection())
                    {
                        var applications = new List<Application>();
                        for (int i = 0; i < 5000; i++)
                        {
                            applications.Add(new Application
                            {
                                Name = Guid.NewGuid().ToString(),
                                CreateTime = DateTime.Now
                            });
                        }

                        applications.Add(new Application
                        {
                            Name = uniqueName,
                            CreateTime = DateTime.Now
                        });
                        applications.Add(new Application
                        {
                            Name = uniqueName,
                            CreateTime = DateTime.Now
                        });
                        var sw = new Stopwatch();
                        sw.Start();
                        try
                        {
                            await connection.InsertAsync(applications.ToArray());
                        }
                        finally
                        {
                            sw.Stop();
                            _outputHelper.WriteLine($"ElapsedMilliseconds:{sw.ElapsedMilliseconds}");
                        }            
                    }
                    scope.Complete();
                }
            });
        }
    }
}
