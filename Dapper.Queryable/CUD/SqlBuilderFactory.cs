using System;

namespace Dapper.Queryable.CUD
{
    internal static class SqlBuilderFactory
    {
        public static object Factory(Type type, SqlOperation operation)
        {
            return InstanceOpenration(type, operation);
        }

        private static object InstanceOpenration(Type type, SqlOperation operation)
        {
            switch (operation)
            {
                case SqlOperation.Insert:
                    return new SqlOperationInsert(type).InsertBuild();
                case SqlOperation.Update:
                    return new SqlOperationUpdate(type).UpdateBuild();
                case SqlOperation.Delete:
                    return new SqlOperationDelete(type).DeleteBuild();
                default:
                    return null;
            }
        }
    }
}
