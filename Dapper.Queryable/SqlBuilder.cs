using System;
using System.Collections.Concurrent;
using System.Linq;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.CUD;
using Dapper.Queryable.Queryable;
using Dapper.Queryable.Utils;

namespace Dapper.Queryable
{
    public class SqlBuilder : ISqlBuilder
    {
        private static readonly ConcurrentDictionary<string, Delegate> ModuleHandles =
            new ConcurrentDictionary<string, Delegate>();

        public string Insert(Type type)
        {
            bool mutil = false;
            if (type.IsArray)
            {
                mutil = true;
                type = type.GetElementType();
                if (type == null)
                {
                    throw new ArgumentException("Type GetElementType Null");
                }
            }

            string key = type.FullName + SqlOperation.Insert;
            if (!ModuleHandles.TryGetValue(key, out var @delegate))
            {
                @delegate = (Delegate) SqlBuilderFactory.Factory(type, SqlOperation.Insert);
                if (@delegate == null)
                {
                    throw new ArgumentNullException(type.FullName + "Insert delegate is null");
                }

                ModuleHandles.TryAdd(key, @delegate);
            }

            var func = (Func<string>) @delegate;
            var str = func?.Invoke() ?? String.Empty;
            return mutil ? str.Split(';')[0] : str;
        }

        public string Update(Type type)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
                if (type == null)
                {
                    throw new ArgumentException("Type GetElementType Null");
                }
            }

            string key = type.FullName + SqlOperation.Update;
            if (!ModuleHandles.TryGetValue(key, out var @delegate))
            {
                @delegate = (Delegate) SqlBuilderFactory.Factory(type, SqlOperation.Update);
                if (@delegate == null)
                {
                    throw new ArgumentNullException(type.FullName + "Update delegate is null");
                }
                   
                ModuleHandles.TryAdd(key, @delegate);
            }

            var func = (Func<string>) @delegate;
            return func?.Invoke();
        }

        public string Delete(Type type)
        {
            if (type.IsArray)
            {
                type = type.GetElementType();
                if (type == null)
                {
                    throw new ArgumentException("Type GetElementType Null");
                }
            }

            string key = type.FullName + SqlOperation.Delete;
            if (!ModuleHandles.TryGetValue(key, out var @delegate))
            {
                @delegate = (Delegate) SqlBuilderFactory.Factory(type, SqlOperation.Delete);
                if (@delegate == null)
                {
                    throw new ArgumentNullException(type.FullName + "Delete delegate is null");
                }

                ModuleHandles.TryAdd(key, @delegate);
            }

            var func = (Func<string>) @delegate;
            return func?.Invoke();
        }

        private static Clause BuildClause<TModel>(IQuery<TModel> query)
        {
            var func = QueryHandlers.TryGet<TModel>(query.GetType());
            if (func == null)
                throw new ArgumentNullException(nameof(query));

            var clause = func(query);
            return clause;
        }

        private string BuildPageSql<TModel>(IQuery<TModel> query, Clause clause)
        {
            var descriptor = TableCache.GetTableDescriptor(typeof(TModel));
            if (descriptor == null)
                throw new ArgumentException("TableName");

            clause.Paging = true;

            var options = descriptor.Options;

            if (query.OrderBys == null || query.OrderBys.Count == 0)
            {
                BuildDefaultOrderBy(clause, descriptor);
            }
            
            var sqlBuilder = StringBuilderCache.Acquire();
            sqlBuilder.Append(BuildSelectSql(descriptor));
            sqlBuilder.Append(clause.Where);
            sqlBuilder.Append(clause.OrderBy);
            sqlBuilder.Append(options.GetPageSql(query.Skip ?? 0, query.Take ?? 1));
            sqlBuilder.Append(" ;");

            sqlBuilder.Append("SELECT COUNT(1) from ");
            sqlBuilder.Append(options.StartDelimiter);
            sqlBuilder.Append(descriptor.TableName);
            sqlBuilder.Append(options.EndDelimiter);
            sqlBuilder.Append(clause.Where);
            sqlBuilder.Append(" ;");
            return StringBuilderCache.GetStringAndRelease(sqlBuilder);
        }

        private static void BuildDefaultOrderBy(Clause clause,
            TableDescriptor descriptor)
        {
            var options = descriptor.Options;
            var pks = descriptor.ColumnDescriptors.Where(n => n.IsPrimaryKey).ToList();
            var stringBuilder = StringBuilderCache.Acquire();
            var len = pks.Count;
            stringBuilder.Append(" ORDER BY");
            for (var index = 0; index < len; index++)
            {
                var pk = pks[index];
                stringBuilder.Append(" ");
                stringBuilder.Append(options.StartDelimiter);
                stringBuilder.Append(pk.DbName);
                stringBuilder.Append(options.EndDelimiter);
                stringBuilder.Append(" ");
                stringBuilder.Append(OrderDirection.Desc);
                if (index != len - 1)
                {
                    stringBuilder.Append(",");
                }
            }
            clause.OrderBy = StringBuilderCache.GetStringAndRelease(stringBuilder);
        }

        public Clause SelectAsync<TModel>(IQuery<TModel> query)
        {
            var clause = BuildClause(query);
            if (query.Skip.HasValue || query.Take.HasValue)
            {
                var sql = BuildPageSql(query, clause);
                clause.Sql = sql;
                return clause;
            }

            if (string.IsNullOrEmpty(clause.Where))
            {
                return clause;
            }

            var descriptor = TableCache.GetTableDescriptor(typeof(TModel));
            if (descriptor == null)
                throw new ArgumentException("TableName");

            var sqlBuilder = StringBuilderCache.Acquire();
            sqlBuilder.Append(this.BuildSelectSql(descriptor));
            sqlBuilder.Append(clause.Where);
            sqlBuilder.Append(clause.OrderBy);
            clause.Sql = StringBuilderCache.GetStringAndRelease(sqlBuilder);
            return clause;
        }

        private static string GetColumns(TableDescriptor descriptor)
        {
            var options = descriptor.Options;
            var startDelimiter = options.StartDelimiter;
            var endDelimiter = options.EndDelimiter;
            var len = descriptor.ColumnDescriptors.Count;
            var stringBuilder = StringBuilderCache.Acquire();
            for (var i = 0; i < len; i++)
            {
                var n = descriptor.ColumnDescriptors[i];
                stringBuilder.Append(" ");
                stringBuilder.Append(startDelimiter);
                stringBuilder.Append(n.Name);
                stringBuilder.Append(endDelimiter);
                stringBuilder.Append(" As ");
                stringBuilder.Append(startDelimiter);
                stringBuilder.Append(n.DbName);
                stringBuilder.Append(endDelimiter);
                stringBuilder.Append(" ");
                if (i != len - 1)
                {
                    stringBuilder.Append(",");
                }
            }
            return StringBuilderCache.GetStringAndRelease(stringBuilder);
        }

        private string BuildSelectSql(TableDescriptor descriptor)
        {
            var columns = GetColumns(descriptor);
            var tableName = descriptor.TableName;
            var options = descriptor.Options;
            return options.GetSelectSql(columns, tableName);
        }
    }
}
