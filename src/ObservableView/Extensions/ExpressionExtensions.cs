using System.Linq.Expressions;

namespace ObservableView.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression ToLower(this Expression expression)
        {
            EnsureToString(ref expression);

            var methodInfo = ReflectionHelper<string>.GetMethod(source => source.ToLower());
            var toLowerExpression = Expression.Call(expression, methodInfo);

            expression = IsNotNull(expression, toLowerExpression);
            return expression;
        }

        public static Expression ToUpper(this Expression expression)
        {
            EnsureToString(ref expression);

            var methodInfo = ReflectionHelper<string>.GetMethod(source => source.ToUpper());
            var toUpperExpression = Expression.Call(expression, methodInfo);

            expression = IsNotNull(expression, toUpperExpression);
            return expression;
        }

        public static Expression Trim(this Expression expression)
        {
            EnsureToString(ref expression);

            var methodInfo = ReflectionHelper<string>.GetMethod(source => source.Trim());
            var trimExpression = Expression.Call(expression, methodInfo);

            expression = IsNotNull(expression, trimExpression);
            return expression;
        }

        public static Expression ToStringExpression(this Expression expression)
        {
            var methodInfo = ReflectionHelper<string>.GetMethod(source => source.ToString());
            return Expression.Call(expression, methodInfo);
        }

        private static void EnsureToString(ref Expression expression)
        {
            if (expression.Type != typeof(string))
            {
                expression = expression.ToStringExpression();
            }
        }

        public static Expression IsNotNull(this Expression checkExpression, Expression expressionIfNotNull)
        {
            var expression = Expression.Condition(
                 Expression.NotEqual(checkExpression, Expression.Constant(null, checkExpression.Type)),
                 expressionIfNotNull,
                 Expression.Constant(string.Empty));

            return expression;
        }
    }
}