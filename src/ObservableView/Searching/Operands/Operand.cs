using System.Linq.Expressions;

using ObservableView.Searching.Processors;

namespace ObservableView.Searching.Operands
{
    public abstract class Operand : IOperand
    {
        public abstract Expression Build(IExpressionBuilder expressionBuilder);

        public IExpressionProcessor[] ExpressionProcessors { get; set; }
    }
}