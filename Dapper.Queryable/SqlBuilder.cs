using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Dapper.Queryable.Abstractions.Data;
using Dapper.Queryable.Abstractions.Data.Attributes;
using Dapper.Queryable.CUD;
using Dapper.Queryable.Queryable;

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

        private Clause GetMsSqlStr<TModel>(IQuery<TModel> query)
        {
            var clause = BuildClause(query);
            if (query.Skip.HasValue)
            {
                var sql = BuildMsPageSql(query, clause);
                clause.Sql = sql;
                return clause;
            }
            else
            {
                if (string.IsNullOrEmpty(clause.Where))
                {
                    return clause;
                }

                var sql = $"{this.BuildSelectSql<TModel>(Analyzer.Ms)} {clause.Where}";
                clause.Sql = sql;
                return clause;
            }
        }

        private static Clause BuildClause<TModel>(IQuery<TModel> query)
        {
            var func = QueryHandlers.TryGet<TModel>(query.GetType());
            if (func == null)
                throw new ArgumentNullException(nameof(query));

            var clause = func(query);
            return clause;
        }

        private string BuildMsPageSql<TModel>(IQuery<TModel> query, Clause clause)
        {
            var descriptor = SqlBuilderUtil.GetTableDescriptor(typeof(TModel));
            if (descriptor == null)
                throw new ArgumentException("TableName");

            if (query.OrderBys == null || query.OrderBys.Count == 0)
            {
                var pks = SqlBuilderUtil.GetColumnDescriptors(typeof(TModel))
                    .Where(n => n.IsPrimaryKey)
                    .ToList();

                var sb = new StringBuilder();
                var len = pks.Count;
                sb.Append(" ORDER BY ");
                for (var index = 0; index < len; index++)
                {
                    var pk = pks[index];
                    sb.Append($" [{pk.DbName}] DESC ");
                    if (index != len - 1)
                    {
                        sb.Append(",");
                    }
                }
                clause.OrderBy = sb.ToString();
            }

            clause.Paging = true;
            var wheresql = $"{clause.Where} {clause.OrderBy} offset {query.Skip ?? 0} rows fetch next {query.Take ?? 1} rows only";
            var sql = $"{this.BuildSelectSql<TModel>(Analyzer.Ms)} {wheresql} ; SELECT COUNT(1) from [{descriptor.TableName}] {clause.Where};";
            return sql;
        }

        private Clause GetMySqlStr<TModel>(IQuery<TModel> query)
        {
            var clause = BuildClause(query);
            if (query.Skip.HasValue)
            {
                var sql = BuildMySqlPageSql(query, clause);
                clause.Sql = sql;
                return clause;
            }
            else
            {
                if (string.IsNullOrEmpty(clause.Where))
                {
                    return clause;
                }

                var descriptor = SqlBuilderUtil.GetTableDescriptor(typeof(TModel));
                if (descriptor == null)
                    throw new ArgumentException("TableName");

                var sql = $"{this.BuildSelectSql<TModel>(Analyzer.My)} {clause.Where}";
                clause.Sql = sql;
                return clause;
            }
        }

        private string BuildMySqlPageSql<TModel>(IQuery<TModel> query, Clause clause)
        {
            var descriptor = SqlBuilderUtil.GetTableDescriptor(typeof(TModel));
            if (descriptor == null)
                throw new ArgumentException("TableName");

            if (query.OrderBys == null || query.OrderBys.Count == 0)
            {
                var pks = SqlBuilderUtil.GetColumnDescriptors(typeof(TModel))
                    .Where(n => n.IsPrimaryKey)
                    .ToList();

                var sb = new StringBuilder();
                var len = pks.Count;
                sb.Append(" ORDER BY ");
                for (var index = 0; index < len; index++)
                {
                    var pk = pks[index];
                    sb.Append($"  `{pk.DbName}` DESC ");
                    if (index != len - 1)
                    {
                        sb.Append(",");
                    }
                }
                clause.OrderBy = sb.ToString();
            }

            clause.Paging = true;
            var wheresql = $"{clause.Where} {clause.OrderBy} limit {query.Skip ?? 0},{query.Take ?? 1}";
            var sql = $"{this.BuildSelectSql<TModel>(Analyzer.My)} {wheresql}; SELECT COUNT(1) from `{descriptor.TableName}` {clause.Where};";
            return sql;
        }

        public Clause SelectAsync<TModel>(IQuery<TModel> query)
        {
            var dialect = SqlBuilderUtil.GetDialect(typeof(TModel));
            Clause clause;
            switch (dialect)
            {
                case Analyzer.Ms:
                    clause = this.GetMsSqlStr(query);
                    break;
                case Analyzer.My:
                    clause = this.GetMySqlStr(query);
                    break;
                default:
                    throw new ArgumentException("Analyzer");
            }
            return clause;
        }

        private string BuildSelectSql<TModel>(Analyzer analyzer)
        {
            var descriptor = SqlBuilderUtil.GetTableDescriptor(typeof(TModel));
            if (descriptor == null)
                throw new ArgumentException("TableName");

            var str = string.Empty;
            switch (analyzer)
            {
                case Analyzer.Ms:
                    str = $"SELECT {descriptor.Columns} FROM [{descriptor.TableName}] with(nolock) ";
                    break;
                case Analyzer.My:
                    str = $"SELECT {descriptor.Columns} FROM `{descriptor.TableName}` ";
                    break;
            }

            return str;
        }
    }
}
