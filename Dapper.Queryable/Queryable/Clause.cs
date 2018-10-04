using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Dapper.Queryable.Queryable
{
    public class Clause
    {
        public string Sql { get; set; }

        public string Where { get; set; }

        public string OrderBy { get; set; }

        public DynanicParameters Parameters { get; set; }

        public bool Paging { get; set; }
    }

    public class DynanicParameters
    {
        private readonly List<ParameterInfo> _parameters = new List<ParameterInfo>(16);

        public void Add(string name, object value = null)
        {
            _parameters.Add(new ParameterInfo
            {
                Name = name,
                Value = value
            });
        }

        public IEnumerable<ParameterInfo> GetParameters()
        {
            return _parameters;
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }

        public object Value { get; set; }
    }
}
