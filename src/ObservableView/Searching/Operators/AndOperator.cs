using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;

using ObservableView.Searching.Operands;
using ObservableView.Searching.Operations;

namespace ObservableView.Searching.Operators
{
    // [DataContract(Name = "AndOperator")]
    [DebuggerDisplay("AndOperator")]
    public class AndOperator : GroupOperator
    {
        public override Expression Build(IExpressionBuilder expressionBuilder, Operation operation)
        {
            GroupOperation groupOperation = (GroupOperation)operation;

            return Expression.AndAlso(expressionBuilder.Build(groupOperation.LeftOperation), expressionBuilder.Build(groupOperation.RightOperation));
        }
    }
}