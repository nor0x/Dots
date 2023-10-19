using System.Linq.Expressions;

using ObservableView.Searching.Processors;

namespace ObservableView.Searching.Operands
{
    public interface IOperand
    {
        Expression Build(IExpressionBuilder expressionBuilder);

        IExpressionProcessor[] ExpressionProcessors { get; set; }
    }
}