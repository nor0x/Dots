using System;
using System.Diagnostics;

namespace ObservableView.Searching.Operands
{
    [DebuggerDisplay("VariableOperand: VariableName={VariableName}, Value={Value}")]
    public class VariableOperand : ConstantOperand
    {
        public VariableOperand(string variableName, Type type)
            : base(type)
        {
            this.VariableName = variableName;
        }

        public string VariableName { get; private set; }
    }
}