using System;
using System.Linq.Expressions;

using ObservableView.Extensions;

namespace ObservableView.Sorting
{
    internal class OrderSpecification<T> // TODO GATH: Could be a struct?
    {
        public OrderSpecification(Expression<Func<T, object>> keySelector, OrderDirection orderDirection)
        {
            this.KeySelector = keySelector.Compile();
            this.PropertyName = ReflectionHelper<T>.GetProperty(keySelector).Name;
            this.OrderDirection = orderDirection;
        }

        public string PropertyName { get; private set; }

        public Func<T, object> KeySelector { get; private set; }

        public OrderDirection OrderDirection { get; private set; }
    }
}