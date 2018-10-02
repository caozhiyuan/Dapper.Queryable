using System.Collections.Generic;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Configuration.AnalyzerOptions;

namespace Dapper.Queryable.Configuration
{
    public class SqlDatabaseOptionsFactory
    {
        private static readonly Dictionary<Analyzer, SqlDatabaseOptions> Factory;

        static SqlDatabaseOptionsFactory()
        {
            Factory = new Dictionary<Analyzer, SqlDatabaseOptions>
            {
                {Analyzer.Ms, new MsSqlDatabaseOptions()},
                {Analyzer.My, new MySqlDatabaseOptions()}
            };
        }

        public static SqlDatabaseOptions GetSqlDatabaseOptions(Analyzer analyzer)
        {
            Factory.TryGetValue(analyzer, out var options);
            return options;
        }
    }
}
