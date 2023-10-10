using System.Linq.Expressions;

using ObservableView.Searching.Operands;
using ObservableView.Searching.Operations;

namespace ObservableView.Searching.Operators
{
    public abstract class BinaryOperator : IOperator
    {
        ////private static readonly IEnumerable<Type> derivedTypes; // TODO: MOVE TO BASE

        static BinaryOperator()
        {
            // Initialize all binary operators in dictionary
            ////derivedTypes = FindDerivedTypes<BinaryOperator>().ToList();
        }

        public Expression Build(IExpressionBuilder expressionBuilder, Operation operation)
        {
            var binaryOperation = (BinaryOperation)operation;

            return this.Build(expressionBuilder, binaryOperation);
        }

        public abstract Expression Build(IExpressionBuilder expressionBuilder, BinaryOperation operation);

        ////public static IEnumerable<Type> FindDerivedTypes<T>()
        ////{
        ////    var baseType = typeof(T);
        ////    var typeInfo = baseType.GetTypeInfo();

        ////    return typeInfo.Assembly.ExportedTypes.Where(t => 
        ////                                            t.GetTypeInfo().IsClass &&
        ////                                            t.GetTypeInfo().IsAbstract == false &&
        ////                                            t.GetTypeInfo().IsSubclassOf(baseType));
        ////}

        public static EqualOperator Equal
        {
            get
            {
                return new EqualOperator();
            }
        }

        public static ContainsOperator Contains
        {
            get
            {
                return new ContainsOperator();
            }
        }
    }
}