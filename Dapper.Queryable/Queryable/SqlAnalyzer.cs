using Dapper.Queryable.Queryable.Analyzers;

namespace Dapper.Queryable.Queryable
{
    public class SqlAnalyzer
    {
        private static IAnalyzer[] Analyzers { get; set; }

        public static IAnalyzer[] Create()
        {
            IAnalyzer[] analyzers = Analyzers;
            if (analyzers != null)
                return analyzers;

            return Analyzers = new IAnalyzer[]
            {
                new Equal(),
                new Contains(),
                new StringContains(),
                new Range(),
                new OrderBy()
            };
        }
    }
}
