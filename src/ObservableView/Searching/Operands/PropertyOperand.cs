using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

using ObservableView.Searching.Processors;

namespace ObservableView.Searching.Operands
{
    // [DataContract(Name = "PropertyOperand")]
    [DebuggerDisplay("PropertyOperand: Name={PropertyInfo.Name}, Type={PropertyInfo.PropertyType.Name}")]
    public class PropertyOperand : Operand
    {
        public PropertyOperand(PropertyInfo propertyInfo, IExpressionProcessor[] expressionProcessors = null)
        {
            this.PropertyInfo = propertyInfo;
            this.ExpressionProcessors = expressionProcessors;
        }

        // [DataMember(Name = "PropertyInfo", IsRequired = true)]
        public PropertyInfo PropertyInfo { get; set; }

        public override Expression Build(IExpressionBuilder expressionBuilder)
        {
            Expression propertyExpression = Expression.Property(expressionBuilder.ParameterExpression, this.PropertyInfo);

            // TODO Move ExpressionProcessors to PropertyOperand (not used anywhere else)...
            if (this.ExpressionProcessors != null)
            {
                foreach (var expressionProcessor in this.ExpressionProcessors)
                {
                    propertyExpression = expressionProcessor.Process(propertyExpression);
                }
            }

            return propertyExpression;
        }
    }
}