using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.Configuration;

namespace Dapper.Queryable
{
    internal static class SqlBuilderUtil
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
            TableAttribute table = GetTable(typeOfModel);
            TableDescriptor tableDescriptor = new TableDescriptor
            {
                Columns = GetColumns(typeOfModel),
                TableName = table.Name
            };
            return tableDescriptor;
        }

        private static string GetColumns(Type typeOfModel)
        {
            var cols = GetColumnDescriptors(typeOfModel);

            var dialect = GetDialect(typeOfModel);

            var options = SqlDatabaseOptionsFactory.GetSqlDatabaseOptions(dialect);
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;

            var colStrs = cols.Select(n => $" {startDelimiter}{n.Name}{endDelimiter} As {startDelimiter}{n.DbName}{endDelimiter} ");
            return string.Join(",", colStrs);
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

        public static string GetDialectPatten(Analyzer dialect, SqlOperation operation)
        {
            if (dialect == Analyzer.My)
                return GetMySqlOperationPatten(operation);
            return GetSqlServerOperationPatten(operation);
        }

        private static string GetSqlServerOperationPatten(SqlOperation operation)
        {
            switch (operation)
            {
                case SqlOperation.Insert:
                    return
                        "INSERT INTO [{TableName}] ({Columns}) VALUES ({Values}); select SCOPE_IDENTITY() AS [Id] ";
                case SqlOperation.Update:
                    return "UPDATE [{TableName}] SET {SetColumns} {WhereClause}";
                case SqlOperation.Delete:
                    return "DELETE FROM [{TableName}] {WhereClause}";
                default:
                    return (string) null;
            }
        }

        private static string GetMySqlOperationPatten(SqlOperation operation)
        {
            switch (operation)
            {
                case SqlOperation.Insert:
                    return "INSERT INTO `{TableName}` ({Columns}) VALUES ({Values}); SELECT @@IDENTITY AS `Id` ";
                case SqlOperation.Update:
                    return "UPDATE `{TableName}` SET {SetColumns} {WhereClause}";
                case SqlOperation.Delete:
                    return "DELETE FROM `{TableName}` {WhereClause}";
                default:
                    return (string) null;
            }
        }
    }
}
