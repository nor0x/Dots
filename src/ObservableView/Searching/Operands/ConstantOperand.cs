using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using ObservableView.Extensions;

namespace ObservableView.Searching.Operands
{
    [DebuggerDisplay("ConstantOperand: Value={Value}, Type={Type}")]
    public class ConstantOperand : Operand
    {
        private object value;

        /// <summary>
        /// Takes a <param name="type">target type</param>. The value is not (yet) specified.
        /// </summary>
        public ConstantOperand(Type type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Takes a <param name="value">value</param> of arbitrary data.
        /// </summary>
        public ConstantOperand(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            this.Value = value;
            this.Type = value.GetType();
        }

        /// <summary>
        /// Takes a <param name="value">value</param> of arbitrary data
        /// which will be converted to <param name="type">type</param> at build time.
        /// </summary>
        public ConstantOperand(object value, Type type)
            : this(value)
        {
            this.Type = type;
        }

        public Type Type { get; private set; }

        public object Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
                this.Type = value == null ? typeof(object) : value.GetType();
            }
        }

        public override Expression Build(IExpressionBuilder expressionBuilder)
        {
            Expression constantExpression = null;

            var genericType = this.Type.GetGenericType();
            var convertedValue = TryConvertToTargetType(this.Value, targetType: genericType);
            if (convertedValue != null)
            {
                constantExpression = Expression.Constant(convertedValue, this.Type);
            }
            else
            {
                constantExpression = this.Type.GetDefaultValueExpression();
            }

            return constantExpression;
        }

        private static object TryConvertToTargetType(object value, Type targetType)
        {
            if (targetType != null && value != null)
            {
                try
                {
                    if (targetType.GetTypeInfo().IsEnum)
                    {
                        value = Enum.ToObject(targetType, value);
                    }
                    else
                    {
                        value = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                    }
                }
                catch (FormatException formatException)
                {
                    throw new FormatException(
                        string.Format(CultureInfo.InvariantCulture, "Value {0} does not match type {1}.",
                        value, 
                        targetType),
                        formatException);
                }
            }

            return value;
        }
    }
}