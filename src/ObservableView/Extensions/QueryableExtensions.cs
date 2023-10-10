using System;
using System.Linq;
using System.Linq.Expressions;

namespace ObservableView.Extensions
{
    [Preserve(AllMembers = true)]
    public static class QueryableExtensions
    {
        public static IQueryable<T> Where<T>(this IQueryable<T> source, Expression baseExpression, ParameterExpression parameterExpression)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (baseExpression == null)
            {
                throw new ArgumentNullException("baseExpression");
            }
            if (parameterExpression == null)
            {
                throw new ArgumentNullException("parameterExpression");
            }

            // TODO: Use ReflectionHelper here
            ////MethodInfo whereMethodInfo = ReflectionHelper<IQueryable<T>>.GetMethod<Func<T, bool>>((x, arg) => x.Where(arg));
            ////MethodInfo genericWhereMethodInfo = whereMethodInfo
            ////    .GetGenericMethodDefinition()
            ////    .MakeGenericMethod(source.ElementType);

            ////MethodCallExpression whereCallExpression = Expression.Call(
            ////    null,
            ////    genericWhereMethodInfo, 
            ////    source.Expression,
            ////    Expression.Lambda<Func<T, bool>>(baseExpression, new[] { parameterExpression }));

            MethodCallExpression whereCallExpression = Expression.Call(
                typeof(Queryable),
                nameof(System.Linq.Queryable.Where),
                new[] { source.ElementType },
                source.Expression,
                Expression.Lambda<Func<T, bool>>(baseExpression, new[] { parameterExpression }));

            return source.Provider.CreateQuery<T>(whereCallExpression);
        }
    }
}