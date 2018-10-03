using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Dapper.Queryable.Utils
{
    public class MethodInfoUtil
    {
        public static readonly MethodInfo StringBuilderToString =
            typeof(StringBuilder).GetMethod("ToString", new Type[0]);

        public static readonly MethodInfo StringBuilderAppend = typeof(StringBuilder).GetMethod("Append", new[]
        {
            typeof(string)
        });

        private static BindingFlags BindingFlags = BindingFlags.Static | BindingFlags.NonPublic;
        public static readonly MethodInfo ReplaceMethod = typeof(MethodInfoUtil).GetMethod("Replace", BindingFlags);
        public static readonly MethodInfo SubStringMethod = typeof(MethodInfoUtil).GetMethod("SubString", BindingFlags);

        private static string Replace(string oldstring, string patten, string newstring)
        {
            return oldstring.Replace(patten, newstring);
        }

        private static string SubString(string oldstring)
        {
            if (string.IsNullOrEmpty(oldstring))
                return string.Empty;
            return oldstring.Substring(0, oldstring.Length - 1);
        }

        private static readonly MethodInfo ChangeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[]
        {
            typeof(object),
            typeof(TypeCode)
        });

        public static Action<object, object> MakeFastPropertySetter(Type modelType, PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.SetMethod;

            var parameterT = Expression.Parameter(typeof(object), "x");
            var parameterY = Expression.Parameter(typeof(object), "y");

            var typeCode = Type.GetTypeCode(propertyInfo.PropertyType);
            var parameterTConvert = Expression.Call(ChangeTypeMethod, parameterY, Expression.Constant(typeCode));
            var parameterTProperty = Expression.Convert(parameterTConvert, propertyInfo.PropertyType);
            var parameterTModel = Expression.Convert(parameterT, modelType);

            var newExpression =
                Expression.Lambda<Action<object, object>>(
                    Expression.Call(parameterTModel, setMethod, parameterTProperty),
                    parameterT,
                    parameterY
                );

            return newExpression.Compile();
        }
    }
}
