using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;

using ObservableView.Searching.Operations;

namespace ObservableView.Searching.Operators
{
    // [DataContract(Name = "EqualOperator")]
    [DebuggerDisplay("EqualOperator")]
    public class EqualOperator : BinaryOperator
    {
        public override Expression Build(IExpressionBuilder expressionBuilder, BinaryOperation binaryOperation)
        {
            Expression leftExpression = expressionBuilder.Build(binaryOperation.LeftOperand);
            Expression rightExpression = expressionBuilder.Build(binaryOperation.RightOperand);

            Expression equalExpression = Expression.Equal(leftExpression, rightExpression);
            return equalExpression;
        }
    }
}