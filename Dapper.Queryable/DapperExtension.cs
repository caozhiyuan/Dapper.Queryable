using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Queryable.Abstractions.Data;

namespace Dapper.Queryable
{
    public static class DapperExtension
    {
        private static readonly Lazy<ISqlBuilder> LazySqlBuilder = new Lazy<ISqlBuilder>(() => new SqlBuilder());

        public static async Task<T> SelectFirstOrDefaultAsync<T>(
            this IDbConnection connection, IQuery<T> query) where T : class
        {
            var results = await connection.SelectAsync(query);
            return results.FirstOrDefault();
        }

        public static async Task<IEnumerable<T>> SelectAsync<T>(
            this IDbConnection connection, IQuery<T> query) where T : class
        {
            var clause = LazySqlBuilder.Value.SelectAsync(query);

            if (Debugger.IsAttached)
            {
                Trace.WriteLine(clause.Sql);
            }

            if (string.IsNullOrEmpty(clause.Sql))
            {
                return Enumerable.Empty<T>();
            }

            var dynamicParameters = new DynamicParameters();
            var parameters = clause.Parameters.GetParameters();
            foreach (var parameter in parameters.Values)
            {
                dynamicParameters.Add(parameter.Name, parameter.Value);
            }
            var grid = await connection.QueryMultipleAsync(clause.Sql, dynamicParameters);
            var result = await grid.ReadAsync<T>() ?? Enumerable.Empty<T>();
            if (clause.Paging)
            {
                query.Count = (await grid.ReadAsync<int>()).FirstOrDefault();
            }

            return result;
        }

        public static async Task<int> InsertAsync<T>(this IDbConnection connection, T entity, 
            IDbTransaction transaction = null)
            where T : class
        {
            var type = typeof(T);
            var sql = LazySqlBuilder.Value.Insert(type);
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException("SqlBuilder Return Sql Empty");
            }

            if (Debugger.IsAttached)
            {
                Trace.WriteLine(sql);
            }

            if (type.IsArray)
            {
                return await connection.ExecuteAsync(sql, entity, transaction);
            }

            var multi = await connection.QueryMultipleAsync(sql, entity).ConfigureAwait(false);
            var first = multi.Read<IdInfo>().FirstOrDefault();
            if (first == null)
            {
                throw new Exception("InsertAsync Error");
            }

            var columnDescriptors = TableCache.GetColumnDescriptors(typeof(T));
            var primaryKey = columnDescriptors.FirstOrDefault(n => n.IsPrimaryKey);
            primaryKey?.SetPrimaryKey(entity, first.Id);
            return 1;
        }

        private class IdInfo
        {
            public object Id { get; set; }
        }

        public static Task<int> UpdateAsync<T>(this IDbConnection connection, T entity,
            IDbTransaction transaction = null)
            where T : class
        {
            var sql = LazySqlBuilder.Value.Update(typeof(T));
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException("SqlBuilder Return Sql Empty");
            }

            if (Debugger.IsAttached)
            {
                Trace.WriteLine(sql);
            }

            return connection.ExecuteAsync(sql, entity, transaction);
        }

        public static Task<int> DeleteAsync<T>(this IDbConnection connection, T entity,
            IDbTransaction transaction = null)
            where T : class
        {
            var sql = LazySqlBuilder.Value.Delete(typeof(T));
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException("SqlBuilder Return Sql Empty");
            }

            if (Debugger.IsAttached)
            {
                Trace.WriteLine(sql);
            }

            return connection.ExecuteAsync(sql, entity, transaction);
        }
    }
}
