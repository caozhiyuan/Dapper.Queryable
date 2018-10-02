namespace Dapper.Queryable.Abstractions.Data
{
    using System.Collections.Generic;

    public class AbstractQuery<TModel> : AbstractQuery, IQuery<TModel>
    {

    }

    public abstract class AbstractQuery : IQuery
    {
        public virtual int? Count { get; set; }

        public virtual List<OrderBy> OrderBys { get; set; }

        public virtual int? Skip { get; set; }

        public virtual int? Take { get; set; }
    }
}

