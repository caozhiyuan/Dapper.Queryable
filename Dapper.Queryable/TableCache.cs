using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Configuration;
using Dapper.Queryable.Utils;

namespace Dapper.Queryable
{
    internal static class TableCache
    {
        private static readonly ConcurrentDictionary<Type, TableAttribute> Tables =
            new ConcurrentDictionary<Type, TableAttribute>();

        private static readonly ConcurrentDictionary<Type, List<ColumnDescriptor>> TableColumns =
            new ConcurrentDictionary<Type, List<ColumnDescriptor>>();

        private static readonly Type[] Types =
        {
            typeof(string),
            typeof(int),
            typeof(Decimal),
            typeof(DateTime),
            typeof(long),
            typeof(float),
            typeof(bool),
            typeof(short),
            typeof(char),
            typeof(char?),
            typeof(short?),
            typeof(bool?),
            typeof(float?),
            typeof(long?),
            typeof(Decimal?),
            typeof(int?),
            typeof(DateTime?)
        };

        public static TableDescriptor GetTableDescriptor(Type typeOfModel)
        {
            var table = GetTable(typeOfModel);
            var cols = GetColumnDescriptors(typeOfModel);
            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(table.Analyzer);

            var tableDescriptor = new TableDescriptor
            {
                TableName = table.Name,
                Analyzer = table.Analyzer,
                Db = table.Db,
                ColumnDescriptors = cols,
                Options = options
            };
            return tableDescriptor;
        }

        public static List<ColumnDescriptor> GetColumnDescriptors(Type typeOfModel)
        {
            var cols = TableColumns.GetOrAdd(typeOfModel, n =>
            {
                var columns = new List<ColumnDescriptor>();
                var properties = n.GetProperties();
                foreach (var propertyInfo in properties)
                {
                    if (Types.Contains(propertyInfo.PropertyType) &&
                        !(propertyInfo.GetCustomAttribute(typeof(IgnoreAttribute)) is IgnoreAttribute))
                    {
                        var customAttribute = propertyInfo.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;

                        var isPrimaryKey = false;
                        var autoIncrement = false;
                        Action<object, object> setPrimaryKey = null;
                        var keyAttribute = propertyInfo.GetCustomAttribute(typeof(KeyAttribute)) as KeyAttribute;
                        if (keyAttribute != null)
                        {
                            isPrimaryKey = true;
                            autoIncrement = keyAttribute.AutoIncrement;
                            if (autoIncrement)
                            {
                                setPrimaryKey = MethodInfoUtil.MakeFastPropertySetter(n, propertyInfo);
                            }
                        }
                        var column = new ColumnDescriptor
                        {
                            IsPrimaryKey = isPrimaryKey,
                            AutoIncrement = autoIncrement,
                            SetPrimaryKey = setPrimaryKey,
                            Name = propertyInfo.Name,
                            DbName = propertyInfo.Name
                        };
                        if (customAttribute?.Name != null)
                        {
                            column.DbName = customAttribute.Name;
                        }
                        columns.Add(column);
                    }
                }

                if (columns.Count == 0)
                {
                    throw new ArgumentException($"{typeOfModel.FullName} no cols");
                }

                var pkCount = columns.Count(m => m.IsPrimaryKey);
                if (pkCount == 0)
                {
                    throw new ArgumentException($"{typeOfModel.FullName} no PrimaryKey");
                }

                if (pkCount > 1)
                {
                    throw new ArgumentException($"{typeOfModel.FullName} PrimaryKey Only Support One ");
                }
                return columns;
            });
            return cols;
        }

        public static Analyzer GetDialect(Type type)
        {
            var tableAttr = GetTable(type);

            return tableAttr.Analyzer;
        }

        public static TableAttribute GetTable(Type type)
        {
            if (!Tables.TryGetValue(type, out var tableAttribute))
            {
                var obj = type.GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault();
                tableAttribute = (TableAttribute) obj ?? throw new ArgumentNullException(nameof(type));
                Tables.TryAdd(type, tableAttribute);
            }

            return tableAttribute;
        }
    }
}
