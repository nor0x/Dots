using System;

namespace ObservableView.Searching
{
    /// <summary>
    ///     Class SearchableAttribute.
    ///     This class is used as a marker annotation to select properties which are included in the search expression.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class SearchableAttribute : Attribute
    {
    }
}