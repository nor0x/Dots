using System;
using System.Linq.Expressions;

namespace ObservableView.Searching.Processors
{
    public interface IExpressionProcessor
    {
        Expression Process(Expression expression);
    }
}