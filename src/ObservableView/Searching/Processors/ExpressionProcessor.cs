using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace ObservableView.Searching.Processors
{
    // [DataContract(Name = "ExpressionProcessor")]
    public abstract class ExpressionProcessor : IExpressionProcessor
    {
        public abstract Expression Process(Expression expression);

        public static ToLowerExpressionProcessor ToLower
        {
            get
            {
                return new ToLowerExpressionProcessor();
            }
        }

        public static ToUpperExpressionProcessor ToUpper
        {
            get
            {
                return new ToUpperExpressionProcessor();
            }
        }

        public static TrimExpressionProcessor Trim
        {
            get
            {
                return new TrimExpressionProcessor();
            }
        }
    }
}