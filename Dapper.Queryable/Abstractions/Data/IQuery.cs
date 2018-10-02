namespace Dapper.Queryable.Abstractions.Data
{
    using System.Collections.Generic;

    public interface IQuery<TModel> : IQuery
    {

    }

    public interface IQuery
    {
        int? Count { get; set; }

        List<OrderBy> OrderBys { get; set; }

        int? Skip { get; set; }

        int? Take { get; set; }
    }

    public class Range<T> : Range where T : struct
    {
        public T? Left { get; set; }

        public T? Right { get; set; }
    }

    public class Range
    {
        public bool LeftExclusive { get; set; }

        public bool RightExclusive { get; set; }
    }

    public enum OrderDirection
    {
        Asc = 0,
        Desc = 1
    }

    public class OrderBy
    {
        public OrderDirection OrderDirection { get; set; }

        public string OrderField { get; set; }
    }
}

