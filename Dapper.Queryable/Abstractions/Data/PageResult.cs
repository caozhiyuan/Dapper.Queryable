using System.Collections.Generic;

namespace Dapper.Queryable.Abstractions.Data
{
    public struct PageResult<TModel>
    {
        public int Count { get; set; }

        public IEnumerable<TModel> Items { get; set; }
    }
}
