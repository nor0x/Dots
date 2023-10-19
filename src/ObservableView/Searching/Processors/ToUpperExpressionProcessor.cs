using System.Diagnostics;
using System.Linq.Expressions;

using ObservableView.Extensions;

namespace ObservableView.Searching.Processors
{
    [DebuggerDisplay("ToUpperExpressionProcessor")]
    public class ToUpperExpressionProcessor : ExpressionProcessor
    {
        public override Expression Process(Expression expression)
        {
            Expression toLowerExpression = expression.ToUpper();

            return toLowerExpression;
        }
    }
}