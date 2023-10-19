using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ObservableView.Extensions
{
    internal static class ReflectionHelper<T>
    {
        /// <summary>
        ///     Gets the method represented by the lambda expression.
        /// </summary>
        /// <param name="methodSelector">An expression that invokes a method.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="methodSelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="methodSelector" /> does not represent a method call.</exception>
        /// <returns>The method info represented by the lambda expression.</returns>
        internal static MethodInfo GetMethod(Expression<Action<T>> methodSelector)
        {
            return GetMethodInfo(methodSelector);
        }

        /// <summary>
        ///     Gets the method represented by the lambda expression.
        /// </summary>
        /// <param name="methodSelector">An expression that invokes a method.</param>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <exception cref="ArgumentNullException">The <paramref name="methodSelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="methodSelector" /> does not represent a method call.</exception>
        /// <returns>The method info represented by the lambda expression.</returns>
        internal static MethodInfo GetMethod<T1>(Expression<Action<T, T1>> methodSelector)
        {
            return GetMethodInfo(methodSelector);
        }

        /// <summary>
        ///     Gets the method represented by the lambda expression.
        /// </summary>
        /// <param name="methodSelector">An expression that invokes a method.</param>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <exception cref="ArgumentNullException">The <paramref name="methodSelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="methodSelector" /> does not represent a method call.</exception>
        /// <returns>The method info represented by the lambda expression.</returns>
        internal static MethodInfo GetMethod<T1, T2>(Expression<Action<T, T1, T2>> methodSelector)
        {
            return GetMethodInfo(methodSelector);
        }

        /// <summary>
        ///     Gets the method represented by the lambda expression.
        /// </summary>
        /// <param name="methodSelector">An expression that invokes a method.</param>
        /// <typeparam name="T1">Type of the first argument.</typeparam>
        /// <typeparam name="T2">Type of the second argument.</typeparam>
        /// <typeparam name="T3">Type of the third argument.</typeparam>
        /// <exception cref="ArgumentNullException">The <paramref name="methodSelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="methodSelector" /> does not represent a method call.</exception>
        /// <returns>The method info represented by the lambda expression.</returns>
        internal static MethodInfo GetMethod<T1, T2, T3>(Expression<Action<T, T1, T2, T3>> methodSelector)
        {
            return GetMethodInfo(methodSelector);
        }

        /// <summary>
        ///     Gets the property represented by the lambda expression.
        /// </summary>
        /// <typeparam name="TResult">The type of the property.</typeparam>
        /// <param name="propertySelector">An expression that accesses a property.</param>
        /// <returns>The property info represented by the lambda expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="propertySelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="propertySelector" /> does not represent a property access.</exception>
        internal static PropertyInfo GetProperty<TResult>(Expression<Func<T, TResult>> propertySelector)
        {
            PropertyInfo info = GetMemberInfo(propertySelector) as PropertyInfo;

            return info;
        }

        /// <summary>
        ///     Gets the field represented by the lambda expression.
        /// </summary>
        /// <typeparam name="TResult">The type of the field.</typeparam>
        /// <param name="fieldSelector">An expression that accesses a field.</param>
        /// <returns>The field info represented by the lambda expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="fieldSelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="fieldSelector" /> does not represent a field access.</exception>
        internal static FieldInfo GetField<TResult>(Expression<Func<T, TResult>> fieldSelector)
        {
            FieldInfo info = GetMemberInfo(fieldSelector) as FieldInfo;

            return info;
        }

        /// <summary>
        ///     Gets the method info represented by the lambda expression.
        /// </summary>
        /// <param name="methodSelector">An expression that invokes a method.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="methodSelector" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="methodSelector" /> does not represent a method call.</exception>
        /// <returns>The method info represented by the lambda expression.</returns>
        private static MethodInfo GetMethodInfo(LambdaExpression methodSelector)
        {
            MethodCallExpression callExpression = methodSelector.Body as MethodCallExpression;
            return callExpression.Method;
        }

        /// <summary>
        ///     Gets the member info represented by the lambda expression.
        /// </summary>
        /// <param name="lambdaExpression">An expression that accesses a member.</param>
        /// <returns>The member info represented by the lambda expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="lambdaExpression" /> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="lambdaExpression" /> does not represent a member access.</exception>
        private static MemberInfo GetMemberInfo(LambdaExpression lambdaExpression)
        {
            var memberExpression = GetMemberExpression(lambdaExpression);
            return memberExpression.Member;
        }

        private static MemberExpression GetMemberExpression(LambdaExpression lambdaExpression)
        {
            var member = lambdaExpression.Body as MemberExpression;
            var unary = lambdaExpression.Body as UnaryExpression;
            var memberExpression = member ?? (unary != null ? unary.Operand as MemberExpression : null);

            if (memberExpression == null)
            {
                throw new ArgumentException("'lambdaExpression' should be a member expression");
            }

            return memberExpression;
        }
    }
}