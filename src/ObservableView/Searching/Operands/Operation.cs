using System.Runtime.Serialization;

using ObservableView.Searching.Operators;

namespace ObservableView.Searching.Operands
{
    public abstract class Operation // TODO: Convert this class into an interface
    {
        // [DataMember(Name = "Operator", IsRequired = true)]
        public IOperator Operator { get; set; }
    }
}