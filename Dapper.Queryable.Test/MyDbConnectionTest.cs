using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using MySql.Data.MySqlClient;
using Xunit;
using Xunit.Abstractions;


namespace Dapper.Queryable.Test
{
    public class MyDbConnectionTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public MyDbConnectionTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private static string ConnectionString => $"User ID=root;Password=123456;Host=localhost;Port=3306;Database=test;Pooling=true;Min Pool Size=0;Max Pool Size=100;SslMode=None";
 
        private async Task<IDbConnection> GetOpenConnection()
        {
            var connection = new MySqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
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
                var task = GetMyApplicationById(c);
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

        private async Task<MyApplication> GetMyApplicationById(int id)
        {
            MyApplication application;
            using (var connection = await GetOpenConnection())
            {
                application = await connection.SelectFirstOrDefaultAsync(new MyApplicationQuery()
                {
                    Ids = new[] { id }
                });
            }
            return application;
        }

        [Fact]
        public async Task SelectPagedAsync()
        {
            using (var connection = await GetOpenConnection())
            {
                var app = new MyApplication
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                };
                await connection.InsertAsync(app);

                var applications = await connection.SelectAsync(new MyApplicationQuery()
                {
                    Take = 10,
                    Skip = 0,
                    NamePattern = app.Name
                });
                Assert.NotEmpty(applications);

                applications = await connection.SelectAsync(new MyApplicationQuery()
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
                var app = new MyApplication
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                };
                await connection.InsertAsync(app);
                var application = await connection.SelectFirstOrDefaultAsync(new MyApplicationQuery()
                {
                    Ids = new[] { app.Id }
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
                var app = new MyApplication
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
                await connection.InsertAsync(new MyApplication()
                {
                    Name = Guid.NewGuid().ToString(),
                    CreateTime = DateTime.Now
                });
            }
        }

        [Fact]
        public async Task BatchInsertAsync()
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
            };
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var connection = await GetOpenConnection())
                {
                    var applications = new List<MyApplication>();
                    for (int i = 0; i < 5000; i++)
                    {
                        applications.Add(new MyApplication
                        {
                            Name = Guid.NewGuid().ToString(),
                            CreateTime = DateTime.Now
                        });
                    }
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
        }
    }
}
