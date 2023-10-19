using System;
using System.Linq.Expressions;

using ObservableView.Searching.Operands;

namespace ObservableView.Searching
{
    /// <summary>
    ///     ExpressionBuilder provides a <see cref="IExpressionBuilder" />
    ///     to build expressions for filter operations.
    /// </summary>
    public interface IExpressionBuilder
    {
        /// <summary>
        ///     Gets parameter expression.
        /// </summary>
        /// <value>The parameter expression.</value>
        ParameterExpression ParameterExpression { get; }

        /// <summary>
        ///     Builds the expression for the specified filter operation.
        /// </summary>
        /// <param name="operation">The filter operation.</param>
        /// <returns>The built expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="operation" /> parameter is <c>null</c>.</exception>
        Expression Build(Operation operation);

        /// <summary>
        ///     Builds the expression for the specified filter operand.
        /// </summary>
        /// <param name="operand">The filter operand.</param>
        /// <returns>The built expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="operand" /> parameter is <c>null</c>.</exception>
        Expression Build(IOperand operand);
    }
}