namespace Dapper.Queryable.Abstractions.Data.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class AnalyzerAttribute : Attribute
    {
        public AnalyzerAttribute(Analyzer analyzer)
        {
            this.Analyzer = analyzer;
        }

        public Analyzer Analyzer { get; set; }
    }
}

