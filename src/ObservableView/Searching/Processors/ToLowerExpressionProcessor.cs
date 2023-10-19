using System.Diagnostics;
using System.Linq.Expressions;

using ObservableView.Extensions;

namespace ObservableView.Searching.Processors
{
    [DebuggerDisplay("ToLowerExpressionProcessor")]
    public class ToLowerExpressionProcessor : ExpressionProcessor
    {
        public override Expression Process(Expression expression)
        {
            Expression toLowerExpression = expression.ToLower();

            return toLowerExpression;
        }
    }
}