using System.Diagnostics;
using System.Linq.Expressions;

using ObservableView.Extensions;

namespace ObservableView.Searching.Processors
{
    [DebuggerDisplay("TrimExpressionProcessor")]
    public class TrimExpressionProcessor : ExpressionProcessor
    {
        public override Expression Process(Expression expression)
        {
            Expression trimExpression = expression.Trim();

            return trimExpression;
        }
    }
}