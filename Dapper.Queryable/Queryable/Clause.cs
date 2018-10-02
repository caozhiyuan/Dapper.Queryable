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
        private readonly ConcurrentDictionary<string, ParameterInfo> _parameters = new ConcurrentDictionary<string, ParameterInfo>();

        public void Add(string name, object value = null)
        {
            _parameters.TryAdd(Clean(name), new ParameterInfo
            {
                Name = name,
                Value = value
            });
        }

        public ConcurrentDictionary<string, ParameterInfo> GetParameters()
        {
            return _parameters;
        }


        private static string Clean(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }

        public object Value { get; set; }
    }
}
