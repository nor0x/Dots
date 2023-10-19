using System;
using System.Linq;
using System.Linq.Expressions;

using ObservableView.Searching.Operands;

namespace ObservableView.Searching
{
    public class ExpressionBuilder : IExpressionBuilder
    {
        /// <summary>
        /// Creates a new instance of ExpressionBuilder.
        /// </summary>
        /// <param name="parameterType">The type for which the ParameterExpression will be created.</param>
        public ExpressionBuilder(Type parameterType)
        {
            var name = parameterType.Name.ToCharArray();
            var capitalLetters = name.Where(char.IsUpper).Select(x => x.ToString());
            var parameterName = capitalLetters.Aggregate((a, b) => a + b).ToLower();

            this.ParameterExpression = Expression.Parameter(parameterType, parameterName);
        }

        /// <summary>
        /// Creates a new instance of ExpressionBuilder.
        /// </summary>
        /// <param name="parameterExpression">Represents a named parameter expression.</param>
        public ExpressionBuilder(ParameterExpression parameterExpression)
        {
            this.ParameterExpression = parameterExpression;
        }

        public ParameterExpression ParameterExpression { get; }

        public Expression Build(Operation operation)
        {
            return operation.Operator.Build(this, operation);
        }

        public Expression Build(IOperand operand)
        {
            return operand.Build(this);
        }
    }
}