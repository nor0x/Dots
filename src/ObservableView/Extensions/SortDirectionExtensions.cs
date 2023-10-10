#if NET45
using System.ComponentModel;
using ObservableView.Sorting;

namespace ObservableView.Extensions
{
    public static class SortDirectionExtensions
    {
        public static ListSortDirection? ToSortDirection(this OrderDirection? orderDirection)
        {
            if (orderDirection == null)
            {
                return null;
            }

            if (orderDirection == OrderDirection.Descending)
            {
                return ListSortDirection.Descending;
            }

            return ListSortDirection.Ascending;
        }

        public static OrderDirection? ToSortDirection(this ListSortDirection? listSortDirection)
        {
            if (listSortDirection == null)
            {
                return null;
            }

            if (listSortDirection == ListSortDirection.Descending)
            {
                return OrderDirection.Descending;
            }

            return OrderDirection.Ascending;
        }
    }
}
#endif