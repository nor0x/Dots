using System;

namespace ObservableView.Filtering
{
    public class FilterEventArgs<T> : EventArgs
    {
        public FilterEventArgs(T item)
        {
            this.Item = item;
            this.IsAllowed = true;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is allowed
        ///     Allowed means this item is included in the view returned in ObservableView.View property.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is allowed; otherwise, <c>false</c>.
        /// </value>
        public bool IsAllowed { get; set; }

        /// <summary>
        ///     Gets the current item of the collection.
        /// </summary>
        /// <value>
        ///     The item.
        /// </value>
        public T Item { get; private set; }
    }
}