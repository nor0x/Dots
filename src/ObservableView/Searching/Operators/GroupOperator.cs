using System.Linq.Expressions;
using System.Runtime.Serialization;

using ObservableView.Searching.Operands;

namespace ObservableView.Searching.Operators
{
    // [DataContract(Name = "GroupOperator")]
    public abstract class GroupOperator : IOperator
    {
        public abstract Expression Build(IExpressionBuilder expressionBuilder, Operation operation);

        public static AndOperator And
        {
            get
            {
                return new AndOperator();
            }
        }

        public static OrOperator Or
        {
            get
            {
                return new OrOperator();
            }
        }
    }
}