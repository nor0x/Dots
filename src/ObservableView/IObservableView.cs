using System.ComponentModel;

using ObservableView.Sorting;

namespace ObservableView
{
    internal interface IObservableView
    {
        event PropertyChangedEventHandler PropertyChanged;

        OrderDirection? GetSortSpecification(string propertyName);

        /// <summary>
        /// Adds a new order specification for a certain property of given <param name="propertyName">propertyName</param>.
        /// </summary>
        /// <param name="propertyName">The property name of the column which shall be sorted.</param>
        /// <param name="orderDirection">Order direction in which the selected property shall be sorted.</param>
        void AddOrderSpecification(string propertyName, OrderDirection orderDirection = OrderDirection.Ascending);

        void RemoveOrderSpecification(string propertyName);

        /// <summary>
        /// Removes all order specifications.
        /// </summary>
        void ClearOrderSpecifications();
    }
}