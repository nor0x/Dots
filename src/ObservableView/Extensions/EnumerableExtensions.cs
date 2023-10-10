using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ObservableView.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        ///     To the observable collection.
        /// </summary>
        /// <typeparam name="T">Generic type T.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The resulting ObservableCollection&lt;T&gt;.</returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ObservableCollection<T>(enumerable);
        }
    }
}