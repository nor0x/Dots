#if NET45
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ObservableView.Sorting;

namespace ObservableView.Extensions
{
    public static class ObservableViewExtensions
    {
        private static DataGrid dataGrid;

        public static readonly DependencyProperty ObservableViewProperty = DependencyProperty.RegisterAttached(
            "ObservableView",
            typeof(object),
            typeof(ObservableViewExtensions),
            new UIPropertyMetadata(null, ObservableViewPropertyChanged));

        public static object GetObservableView(DependencyObject obj)
        {
            return obj.GetValue(ObservableViewProperty);
        }

        public static void SetObservableView(DependencyObject obj, object value)
        {
            obj.SetValue(ObservableViewProperty, value);
        }

        private static void ObservableViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            dataGrid = d as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            var oldObservableView = e.OldValue as IObservableView;
            if (oldObservableView != null)
            {
                dataGrid.Loaded -= DataGridLoaded;
                dataGrid.Unloaded -= DataGridUnloaded;
            }

            var newObservableView = e.NewValue as IObservableView;
            if (newObservableView != null)
            {
                dataGrid.Loaded += DataGridLoaded;
                dataGrid.Unloaded += DataGridUnloaded;

                CheckIfItemsSourcePropertyIsNotBound();
                BindObservableViewToItemsSource();
            }
        }

        static void SortDescriptionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("SortDescriptionCollectionChanged: Action={0}", e.Action);

            if (dataGrid == null)
            {
                return;
            }

            var observableView = GetObservableView(dataGrid) as IObservableView;
            if (observableView == null)
            {
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // clear all columns sort directions
                foreach (var column in dataGrid.Columns)
                {
                    column.SortDirection = null;
                }
            }
         
            if (e.NewItems != null)
            {
                observableView.ClearOrderSpecifications();
                var allSortDescriptions = (SortDescriptionCollection)sender;

                // set columns sort directions
                var sortDescriptions = allSortDescriptions.Union(e.NewItems.Cast<SortDescription>()).ToList();
                foreach (SortDescription sortDescription in sortDescriptions) //TODO GATH NewItems only contains NEW ITEMS; We should also consider existing sort orders
                {

                    var orderDirection = sortDescription.Direction == ListSortDirection.Ascending ? OrderDirection.Ascending : OrderDirection.Descending;
                    observableView.AddOrderSpecification(sortDescription.PropertyName, orderDirection);

                    SetSortDirection(sortDescription.PropertyName, sortDescription.Direction);
                }
            }

            if (e.OldItems != null)
            {
                // reset columns sort directions
                foreach (SortDescription descr in e.OldItems)
                {
                    observableView.RemoveOrderSpecification(descr.PropertyName);
                    SetSortDirection(descr.PropertyName, null);
                }
            }
        }

        private static void SetSortDirection(string sortMemberPath, ListSortDirection? direction)
        {
            var column = dataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == sortMemberPath);
            if (column != null)
            {
                column.SortDirection = direction;
            }
        }

        /// <summary>
        /// Check if there is a binding to ItemsSource.
        /// If you use the ObservableView dependency property, you must use ItemsSource at the same time.
        /// </summary>
        private static void CheckIfItemsSourcePropertyIsNotBound()
        {
            var itemsSourceBindingExpression = dataGrid.GetBindingExpression(ItemsControl.ItemsSourceProperty);
            if (itemsSourceBindingExpression != null)
            {
                throw new InvalidOperationException("Dependency property 'ItemsSource' must not have a binding for ObservableView to work properly. " +
                    "Bind to ObservableView instead.");
            }
        }

        /// <summary>
        /// Since we want to bind the ObservableView directly to the ObservableViewProperty,
        /// we have to make sure that the ItemsSourceProperty of the DataGrid is bound programmatically to the ObservableView.View property.
        /// </summary>
        private static void BindObservableViewToItemsSource()
        {
            var observableViewBindingExpression = dataGrid.GetBindingExpression(ObservableViewProperty);
            if (observableViewBindingExpression == null)
            {
                throw new InvalidOperationException("Dependency property 'ObservableView' does not have a valid binding.");
            }

            string viewPropertyBindingName = observableViewBindingExpression.ResolvedSourcePropertyName + ".View";
            var viewPropertyBinding = new Binding(viewPropertyBindingName);
            dataGrid.SetBinding(ItemsControl.ItemsSourceProperty, viewPropertyBinding);
        }

        private static void DataGridUnloaded(object sender, RoutedEventArgs e)
        {
            if (dataGrid == null)
            {
                return;
            }

            var notifyCollectionChanged = dataGrid.Items.SortDescriptions as INotifyCollectionChanged;
            if (notifyCollectionChanged != null)
            {
                notifyCollectionChanged.CollectionChanged -= SortDescriptionCollectionChanged;
            }

            var observableView = GetObservableView(dataGrid) as IObservableView;
            if (observableView == null)
            {
                return;
            }

            observableView.PropertyChanged -= ObservableViewOnPropertyChanged;
        }

        private static void DataGridLoaded(object sender, RoutedEventArgs e)
        {
            if (dataGrid == null)
            {
                return;
            }

            var observableView = GetObservableView(dataGrid) as IObservableView;
            if (observableView == null)
            {
                return;
            }

            observableView.PropertyChanged += ObservableViewOnPropertyChanged;

            // initial sync
            SyncObservableViewSortWithDataGridSort(observableView, isInitialSync: true);

            // Subscribe column sort changed
            var notifyCollectionChanged = dataGrid.Items.SortDescriptions as INotifyCollectionChanged;
            if (notifyCollectionChanged != null)
            {
                notifyCollectionChanged.CollectionChanged += SortDescriptionCollectionChanged;
            }
        }

        private static void ObservableViewOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "View")
            {
                var observableView = sender as IObservableView;
                if (observableView == null)
                {
                    return;
                }

                SyncObservableViewSortWithDataGridSort(observableView);
            }
        }

        /// <summary>
        ///     This method is used to synchronize the ObservableView's sort specification
        ///     with the DataGrid's sort specification.
        ///     This is the case if the ObservableView refreshes its data.
        ///     (For some mysterious reasons, the DataGrid loses its sort specification in this case)
        /// </summary>
        private static void SyncObservableViewSortWithDataGridSort(IObservableView observableView, bool isInitialSync = false)
        {
            Console.WriteLine("Sync ObservableView => DataGrid");

            if (isInitialSync)
            {
                dataGrid.Items.SortDescriptions.Clear();
            }

            foreach (var dataGridColumn in dataGrid.Columns)
            {
                var listSortDirection = observableView.GetSortSpecification(dataGridColumn.SortMemberPath).ToSortDirection();
                if (listSortDirection != null)
                {
                    dataGridColumn.SortDirection = listSortDirection;
                    if (isInitialSync)
                    {
                        dataGrid.Items.SortDescriptions.Add(new SortDescription(dataGridColumn.SortMemberPath, listSortDirection.Value));
                    }
                }
            }
        }
    }
}
#endif