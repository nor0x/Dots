using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ObservableView.Extensions
{
    internal static class TypeExtensions
    {
        public static Type GetGenericType(this Type type)
        {
            if (type.IsNullable())
            {
                type = type.GetTypeInfo().GenericTypeArguments[0];
            }
            return type;
        }

        public static bool IsNullable(this Type type)
        {
            return type != null && type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Expression GetDefaultValueExpression(this Type type)
        {
            Expression defaultValueExpression = null;
            if (type.GetTypeInfo().IsValueType) // This distinction is obsolete but helps speeding up at execution time
            {
                defaultValueExpression = Expression.Default(type);
            }
            else
            {
                defaultValueExpression = Expression.Constant(null, type);
            }

            return defaultValueExpression;
        }
    }
}