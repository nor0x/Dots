using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using ObservableView.Extensions;
using ObservableView.Searching.Operations;

namespace ObservableView.Searching.Operators
{
    [DebuggerDisplay("ContainsOperator")]
    public class ContainsOperator : BinaryOperator
    {
        private readonly StringComparison stringComparison;

        public ContainsOperator(StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            this.stringComparison = stringComparison;
        }

        public override Expression Build(IExpressionBuilder expressionBuilder, BinaryOperation binaryOperation)
        {
            var leftExpression = expressionBuilder.Build(binaryOperation.LeftOperand);

            Expression leftNotNullExpression = null;
            if (leftExpression.Type.GetTypeInfo().IsValueType == false)
            {
                var nullExpression = Expression.Constant(null, leftExpression.Type);
                leftNotNullExpression = Expression.NotEqual(leftExpression, nullExpression);
            }

            if (leftExpression.Type != typeof(string))
            {
                leftExpression = leftExpression.ToStringExpression();  // TODO : Move to BinaryStringOperator
            }

            var rightExpression = expressionBuilder.Build(binaryOperation.RightOperand);

            Expression rightNotNullExpression = null;
            if (rightExpression.Type.GetTypeInfo().IsValueType == false)
            {
                var nullExpression = Expression.Constant(null, rightExpression.Type);
                rightNotNullExpression = Expression.NotEqual(rightExpression, nullExpression);
            }

            if (rightExpression.Type != typeof(string))
            {
                rightExpression = rightExpression.ToStringExpression(); // TODO : Move to BinaryStringOperator
            }

            MethodInfo containsExtensionMethodInfo = typeof(StringExtensions).GetRuntimeMethod(nameof(StringExtensions.Contains), new[] { typeof(string), typeof(string), typeof(StringComparison) });
            //MethodInfo containsMethodInfo = ReflectionHelper<string>.GetMethod<string>((source, argument) => source.Contains(argument, this.stringComparison));
            var stringComparisonExpression = Expression.Constant(this.stringComparison);

//#if IOS
//            Expression containsExpression = Expression.Call(
//                leftExpression, 
//                containsExtensionMethodInfo,
//                rightExpression,
//                stringComparisonExpression);
           
//#else
            Expression containsExpression = Expression.Call(
                null,
                containsExtensionMethodInfo,
                leftExpression,
                rightExpression,
                stringComparisonExpression);
//#endif

            if (leftNotNullExpression != null)
            {
                containsExpression = Expression.AndAlso(leftNotNullExpression, containsExpression);
            }

            if (rightNotNullExpression != null)
            {
                containsExpression = Expression.AndAlso(rightNotNullExpression, containsExpression);
            }

            return containsExpression;
        }
    }
}