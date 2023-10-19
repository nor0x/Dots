using System.Linq.Expressions;

using ObservableView.Searching.Operands;

namespace ObservableView.Searching.Operators
{
    public interface IOperator
    {
        /// <summary>
        /// Builds the expression of the <paramref name="operation"/> parameter.
        /// </summary>
        /// <param name="expressionBuilder">The expression builder.</param>
        /// <param name="operation">The operation.</param>
        /// <returns>The expression.</returns>
        Expression Build(IExpressionBuilder expressionBuilder, Operation operation);
    }
}