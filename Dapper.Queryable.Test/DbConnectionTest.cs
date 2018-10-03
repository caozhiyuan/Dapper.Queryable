using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Transactions;
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

        private static string ConnectionString => $"Data Source=.;Initial Catalog=systest;Integrated Security=True";

        private async Task<IDbConnection> GetOpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        [Fact]
        public async Task SelectIdsAsync()
        {
            using (var connection = await GetOpenConnection())
            {
                var application = await connection.SelectFirstOrDefaultAsync(new ApplicationQuery()
                {
                    Ids = new[] { 0 }
                });
                Assert.Null(application);
            }
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

                var applications = await connection.SelectAsync(new ApplicationQuery()
                {
                    Take = 10,
                    Skip = 0,
                    NamePattern = app.Name
                });
                Assert.NotEmpty(applications);

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
