using System.Runtime.Serialization;

using ObservableView.Searching.Operands;
using ObservableView.Searching.Operators;

namespace ObservableView.Searching.Operations
{
    public class BinaryOperation : Operation
    {
        public BinaryOperation(BinaryOperator binaryOperator, PropertyOperand leftOperand, IOperand rightOperand)
        {
            this.Operator = binaryOperator;
            this.LeftOperand = leftOperand;
            this.RightOperand = rightOperand;
        }

        // [DataMember(Name = "LeftOperand", IsRequired = true)]
        public IOperand LeftOperand { get; set; }

        // [DataMember(Name = "RightOperand", IsRequired = true)]
        public IOperand RightOperand { get; set; }
    }
}