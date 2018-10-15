using System;
using Dapper.Queryable.Abstractions.Data.Attributes;

namespace Dapper.Queryable.Queryable
{
    public class AnalyzerFactory
    {
        public static IAnalyzer[] GetAnalyzer(Analyzer analyzer)
        {
            switch (analyzer)
            {
                case Analyzer.Ms:
                case Analyzer.My:
                    return SqlAnalyzer.Create();
                default:
                    throw new ArgumentException(nameof(analyzer));
            }
        }
    }
}
