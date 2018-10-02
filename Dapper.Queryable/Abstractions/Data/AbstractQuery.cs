namespace Dapper.Queryable.Abstractions.Data
{
    using System.Collections.Generic;

    public class AbstractQuery<TModel> : AbstractQuery, IQuery<TModel>
    {

    }

    public abstract class AbstractQuery : IQuery
    {
        public int? Count { get; set; }

        public List<OrderBy> OrderBys { get; set; }

        public int? Skip { get; set; }

        public int? Take { get; set; }
    }
}

