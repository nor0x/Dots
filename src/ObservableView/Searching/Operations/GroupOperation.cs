using System.Runtime.Serialization;

using ObservableView.Searching.Operands;
using ObservableView.Searching.Operators;

namespace ObservableView.Searching.Operations
{
    // [DataContract(Name = "GroupOperation")]
    public class GroupOperation : Operation
    {
        public GroupOperation(Operation leftOperation, Operation rightOperation, GroupOperator groupOperator)
        {
            this.LeftOperation = leftOperation;
            this.RightOperation = rightOperation;
            this.Operator = groupOperator;
        }

        // [DataMember(Name = "LeftOperation", IsRequired = true)]
        public Operation LeftOperation { get; set; }

        // [DataMember(Name = "RightOperation", IsRequired = true)]
        public Operation RightOperation { get; set; }
    }
}