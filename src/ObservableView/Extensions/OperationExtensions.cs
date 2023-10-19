using System.Collections.Generic;

using ObservableView.Searching.Operands;
using ObservableView.Searching.Operations;

namespace ObservableView.Extensions
{
    internal static class OperationExtensions
    {
        internal static IEnumerable<object> Flatten(this Operation operation)
        {
            return Recurse(operation);
        }

        private static IEnumerable<object> Recurse(object obj)
        {
            var binaryOperation = obj as BinaryOperation;
            if (binaryOperation != null)
            {
                yield return binaryOperation.LeftOperand;
                yield return binaryOperation.Operator;
                yield return binaryOperation.RightOperand;
            }

            var groupOperation = obj as GroupOperation;
            if (groupOperation != null)
            {
                foreach (var groupObject in RecurseGroupOperation(groupOperation))
                {
                    yield return groupObject;
                }
            }
        }

        private static IEnumerable<object> RecurseGroupOperation(GroupOperation groupOperation)
        {
            foreach (var binObject in Recurse(groupOperation.LeftOperation))
            {
                yield return binObject;
            }

            yield return groupOperation.Operator;

            foreach (var binObject in Recurse(groupOperation.RightOperation))
            {
                yield return binObject;
            }
        }
    }
}