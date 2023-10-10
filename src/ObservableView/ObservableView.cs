using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using ObservableView.Exceptions;
using ObservableView.Extensions;
using ObservableView.Filtering;
using ObservableView.Grouping;
using ObservableView.Searching;
using ObservableView.Sorting;

namespace ObservableView
{
    /// <summary>
    ///     ObservableView is a class which adds sorting, filtering, searching and grouping
    ///     on top of collections.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ObservableView<T> : ObservableObject, IObservableView
    {
        private static readonly object FilterHandlerEventLock = new object();

        private readonly List<OrderSpecification<T>> orderSpecifications;
        private ObservableCollection<T> sourceCollection;

        private string searchText = string.Empty;
        private FilterEventHandler<T> filterHandler;
        private Func<T, object> groupKey;
        private IGroupKeyAlgorithm groupKeyAlgorithm;
        private Func<string, string> searchTextPreprocessor;

        public ObservableView(ObservableCollection<T> collection)
        {
            this.Source = collection;

            this.orderSpecifications = new List<OrderSpecification<T>>();

            this.SearchSpecification = new SearchSpecification<T>();
            this.InitializeSearchSpecificationFromSearchableAttributes();

            this.SearchSpecification.SearchSpecificationAdded += this.OnSearchSpecificationChanged;
            this.SearchSpecification.SearchSpecificationsCleared += this.OnSearchSpecificationsCleared;

            this.GroupKeyAlgorithm = new AlphaGroupKeyAlgorithm();

            this.SearchTextDelimiters = new[] { ' ' };
            this.SearchTextLogic = SearchLogic.Or;
        }

        private void InitializeSearchSpecificationFromSearchableAttributes()
        {
            MethodInfo addMethod = ReflectionHelper<ISearchSpecification<T>>.GetMethod(source => source.Add<T>(null, null, null));

            var searchableAttributes = this.GetSearchableAttributes();
            foreach (var propertyInfo in searchableAttributes)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, propertyInfo);
                var funcType = typeof(Func<,>).MakeGenericType(typeof(T), propertyInfo.PropertyType);
                LambdaExpression lambdaExpression = Expression.Lambda(funcType, property, parameter);

                addMethod.GetGenericMethodDefinition()
                         .MakeGenericMethod(propertyInfo.PropertyType)
                         .Invoke(this.SearchSpecification, new object[] { lambdaExpression, null, null });
            }
        }

        public ObservableView()
            : this(new ObservableCollection<T>())
        {
        }

        public ObservableView(IEnumerable<T> list)
            : this(list.ToObservableCollection())
        {
        }

        public event FilterEventHandler<T> FilterHandler
        {
            add
            {
                lock (FilterHandlerEventLock)
                {
                    this.filterHandler += value;
                }
                this.Refresh();
            }
            remove
            {
                lock (FilterHandlerEventLock)
                {
                    this.filterHandler -= value;
                }
                this.Refresh();
            }
        }

        public Func<T, object> GroupKey
        {
            get => this.groupKey;
            set
            {
                this.groupKey = value;
                this.OnPropertyChanged(() => this.Groups);
            }
        }

        public IGroupKeyAlgorithm GroupKeyAlgorithm
        {
            get => this.groupKeyAlgorithm;
            set
            {
                this.groupKeyAlgorithm = value;
                this.OnPropertyChanged(() => this.Groups);
            }
        }

        public IEnumerable<Grouping<T>> Groups
        {
            get
            {
                if (this.GroupKey == null || this.GroupKeyAlgorithm == null)
                {
                    return Enumerable.Empty<Grouping<T>>();
                }

                var groupedList = this.View
                        .GroupBy(item => this.GroupKeyAlgorithm.GetGroupKey(this.GroupKey.Invoke(item)))
                        .Select(itemGroup => new Grouping<T>(itemGroup.Key, itemGroup))
                        .ToList();

                return groupedList;
            }
        }

        /// <summary>
        ///     Gets or sets the search text.
        ///     This property can be used for data binding and has the same effect
        ///     as using the <code>Search("searchtext")</code> method to perform a search operation.
        /// </summary>
        public string SearchText
        {
            get
            {
                return this.searchText;
            }
            set
            {
                if (this.searchText != value)
                {
                    this.searchText = value;
                    this.OnPropertyChanged(() => this.SearchText);

                    // Update properties to reflect the search result
                    this.OnPropertyChanged(() => this.View);
                    this.OnPropertyChanged(() => this.Groups);
                }
            }
        }

        public Func<string, string> SearchTextPreprocessor
        {
            get
            {
                return this.searchTextPreprocessor;
            }
            set
            {
                this.searchTextPreprocessor = value;
                this.OnPropertyChanged(() => this.Groups);
            }
        }

        public event EventHandler<NotifyCollectionChangedEventArgs> SourceCollectionChanged;

        public ObservableCollection<T> Source
        {
            get
            {
                return this.sourceCollection;
            }
            set
            {
                // Remove previous collection changed event
                if (this.sourceCollection != null)
                {
                    this.sourceCollection.CollectionChanged -= this.HandleSourceCollectionChanged;
                }

                this.sourceCollection = value;

                // Subscribe to collection changed event
                if (this.sourceCollection != null)
                {
                    this.sourceCollection.CollectionChanged += this.HandleSourceCollectionChanged;
                }

                this.HandleSourceCollectionChanged(this.sourceCollection, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void HandleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.Refresh();

            var handler = this.SourceCollectionChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        public ObservableCollection<T> View
        {
            get
            {
                // View returns the original collection if no filtering, search and ordering is applied.
                // The order of processing is set-up in a way to guarantee maximum performance.

                var viewCollection = this.Source;
                if (viewCollection != null && viewCollection.Any())
                {
                    if (!string.IsNullOrEmpty(this.SearchText))
                    {
                        var processedSearchText = this.PreprocessSearchText(this.SearchText);
                        viewCollection = this.PerformSearch(viewCollection, processedSearchText);
                    }

                    if (this.filterHandler != null)
                    {
                        viewCollection = this.GetFilteredCollection(viewCollection);
                    }

                    if (this.orderSpecifications != null && this.orderSpecifications.Any())
                    {
                        viewCollection = PerformOrdering(viewCollection, this.orderSpecifications).ToObservableCollection();
                    }
                }

                // It is important to return the viewCollection in a new ObservableCollection object.
                // Otherwise the binding is not refreshed when OnPropertyChanged is called.
                return new ObservableCollection<T>(viewCollection);
            }
        }

        private string PreprocessSearchText(string text)
        {
            if (this.SearchTextPreprocessor != null)
            {
                try
                {
                    return this.SearchTextPreprocessor(text);
                }
                catch (Exception ex)
                {
                    throw new SearchTextPreprocessorException("Failed to preprocess search string.", ex);
                }
            }

            return text;
        }

        /// <summary>
        /// Adds a new order specification for a certain property of given type T.
        /// </summary>
        /// <param name="keySelector">Lambda expression to select the ordering property.</param>
        /// <param name="orderDirection">Order direction in which the selected property shall be sorted.</param>
        public void AddOrderSpecification(Expression<Func<T, object>> keySelector, OrderDirection orderDirection = OrderDirection.Ascending)
        {
            this.AddOrderSpecificationInternal(keySelector, orderDirection);
            this.Refresh();
        }

        /// <inheritdoc />
        void IObservableView.AddOrderSpecification(string propertyName, OrderDirection orderDirection = OrderDirection.Ascending)
        {
            this.AddOrderSpecification(propertyName, orderDirection);
        }

        /// <inheritdoc />
        void IObservableView.RemoveOrderSpecification(string propertyName)
        {
            this.orderSpecifications.RemoveAll(x => x.PropertyName == propertyName);
        }

        public void AddOrderSpecification(string propertyName, OrderDirection orderDirection = OrderDirection.Ascending)
        {
            var parameter = Expression.Parameter(typeof(T));
            var memberExpression = Expression.Property(parameter, propertyName);
            var keySelector = Expression.Lambda<Func<T, object>>(memberExpression, parameter);

            this.AddOrderSpecificationInternal(keySelector, orderDirection);
        }

        private void AddOrderSpecificationInternal(Expression<Func<T, object>> keySelector, OrderDirection orderDirection)
        {
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            var newOrderSpecification = new OrderSpecification<T>(keySelector, orderDirection);
            var index = this.orderSpecifications.FindIndex(x => x.PropertyName == newOrderSpecification.PropertyName);
            if (index > -1)
            {
                this.orderSpecifications[index] = newOrderSpecification;
            }
            else
            {
                this.orderSpecifications.Add(newOrderSpecification);
            }
        }

        /// <inheritdoc />
        OrderDirection? IObservableView.GetSortSpecification(string propertyName)
        {
            var orderSpecification = this.orderSpecifications.SingleOrDefault(s => s.PropertyName == propertyName);
            return orderSpecification != null ? orderSpecification.OrderDirection : (OrderDirection?)null;
        }

        /// <inheritdoc />
        void IObservableView.ClearOrderSpecifications()
        {
            this.orderSpecifications.Clear();
        }

        /// <inheritdoc />
        public void ClearOrderSpecifications()
        {
            this.orderSpecifications.Clear();

            this.Refresh();
        }

        /// <summary>
        ///     Refreshes the Source, View and Groups property of this instance.
        /// </summary>
        public void Refresh()
        {
            this.OnPropertyChanged(() => this.Source);
            this.OnPropertyChanged(() => this.View);
            this.OnPropertyChanged(() => this.Groups);
        }

        ////private Expression AddExpression(ParameterExpression parameterExpression, string propertyName, string value)
        ////{
        ////    Expression returnExpression = null;
        ////    Expression rightExpression = Expression.Constant(value.ToLower());

        ////    var propertyInfo = typeof(T).GetRuntimeProperty(propertyName);
        ////    if (propertyInfo != null)
        ////    {
        ////        Expression left = Expression.Property(parameterExpression, propertyInfo);
        ////        if (left.Type == typeof(string))
        ////        {
        ////            // If the given property is of type string, we want to compare them in lower letters.
        ////            Expression toLowerExpression = left.ToLower();
        ////            Expression removeDiacriticsExpression = Expression.Call(null, typeof(StringExtensions).GetRuntimeMethod("RemoveDiacritics", new[] { typeof(string) }), toLowerExpression);
        ////            Expression containsExpression = removeDiacriticsExpression.Contains(rightExpression);
        ////            returnExpression = Expression.OrElse(containsExpression, toLowerExpression.Contains(rightExpression)); // There are two comparisons done: One with diacritics and one without.
        ////        }
        ////        else if (left.Type == typeof(int))
        ////        {
        ////            // If the given property is of type integer, we want to convert it to string first.
        ////            Expression leftToLower = Expression.Call(left, typeof(int).GetRuntimeMethod("ToString", new Type[] { })); // TODO: use ToLower extension method
        ////            returnExpression = leftToLower.Contains(rightExpression);
        ////        }
        ////        else if (left.Type.GetTypeInfo().IsEnum)
        ////        {
        ////            // TODO: Handle enum localized strings
        ////        }
        ////    }

        ////    return returnExpression;
        ////}

        ////private Expression CreateToLowerContainsExpression(Expression leftExpression, Expression rightExpression)
        ////{
        ////    Expression leftToLower = Expression.Call(leftExpression, typeof(string).GetRuntimeMethod("ToLower", new Type[] { }));
        ////    return Expression.Call(leftToLower, typeof(string).GetRuntimeMethod("Contains", new[] { typeof(string) }), rightExpression);
        ////}

        private ObservableCollection<T> GetFilteredCollection(IEnumerable<T> viewCollection)
        {
            var filteredCollection = new ObservableCollection<T>();
            foreach (T item in viewCollection)
            {
                var filterEventArgs = new FilterEventArgs<T>(item);
                this.filterHandler(this, filterEventArgs);

                if (filterEventArgs.IsAllowed)
                {
                    filteredCollection.Add(item);
                }
            }

            return filteredCollection;
        }

        public ISearchSpecification<T> SearchSpecification { get; private set; }

        private void OnSearchSpecificationChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private void OnSearchSpecificationsCleared(object sender, EventArgs e)
        {
            this.SearchText = string.Empty;
            this.Refresh();
        }

        private IEnumerable<PropertyInfo> GetSearchableAttributes()
        {
            return typeof(T).GetRuntimeProperties().Where(propertyInfo => propertyInfo.CustomAttributes.Any(attr => attr.AttributeType == typeof(SearchableAttribute))).ToList();
        }

        /// <summary>
        ///     Performs a search operation using the given
        ///     <param name="pattern">search pattern</param>
        ///     .
        ///     The search operation is performed on the properties <code>View</code> and <code>Group</code>.
        /// </summary>
        /// <param name="pattern"></param>
        public void Search(string pattern)
        {
            this.SearchText = pattern;
        }

        public void ClearSearch()
        {
            this.Search(string.Empty);
        }

        /// <summary>
        /// Defines the characters which are relevant for splitting the SearchText
        /// into search words.
        /// </summary>
        public char[] SearchTextDelimiters { get; set; }

        /// <summary>
        /// Defines the logic to be applied between the splitted search words.
        /// </summary>
        public SearchLogic SearchTextLogic { get; set; }

        private ObservableCollection<T> PerformSearch(IEnumerable<T> viewCollection, string pattern)
        {
            var results = new ObservableCollection<T>();

            if (string.IsNullOrEmpty(pattern))
            {
                return viewCollection.ToObservableCollection();
            }

            var searchStrings = pattern.Trim().Split(this.SearchTextDelimiters, StringSplitOptions.RemoveEmptyEntries);
            if (!searchStrings.Any())
            {
                return results;
            }

            if (!this.SearchSpecification.Any())
            {
                throw new InvalidOperationException(
                    "Please add at least one search specification either by calling SearchSpecification.Add " +
                    $"or by defining [Searchable] annotations on properties in type {typeof(T).Name} to mark them as searchable.");
            }

            ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "x");
            Expression baseExpression = this.BuildBaseExpression(parameterExpression, searchStrings, this.SearchTextLogic);
            if (baseExpression == null)
            {
                return results;
            }

            IQueryable<T> queryableDtos = viewCollection.AsQueryable();
            return queryableDtos.Where(baseExpression, parameterExpression).ToObservableCollection();
        }

        /// <summary>
        ///     This methods builds the Expression tree from the defined SearchSpecification and the given searchStrings.
        /// </summary>
        private Expression BuildBaseExpression(ParameterExpression parameterExpression, string[] searchStrings, SearchLogic searchLogic)
        {
            Expression baseExpression = null;
            foreach (var searchString in searchStrings)
            {
                this.SearchSpecification.ReplaceSearchTextVariables(searchString);

                var expressionBuilder = new ExpressionBuilder(parameterExpression);
                var lastExpression = expressionBuilder.Build(this.SearchSpecification.BaseOperation);
                if (lastExpression != null)
                {
                    if (baseExpression == null)
                    {
                        baseExpression = lastExpression;
                    }
                    else
                    {
                        if (searchLogic == SearchLogic.Or)
                        {
                            baseExpression = Expression.OrElse(baseExpression, lastExpression);
                        }
                        else if (searchLogic == SearchLogic.And)
                        {
                            baseExpression = Expression.AndAlso(baseExpression, lastExpression);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Provided searchLogic '{searchLogic}' is not valid.");
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return baseExpression;
        }

        private static IEnumerable<T> PerformOrdering(IEnumerable<T> enumerable, IEnumerable<OrderSpecification<T>> orderSpecifications)
        {
            IQueryable<T> query = enumerable.AsQueryable();

            OrderSpecification<T> firstSpecification = orderSpecifications.First();
            IOrderedEnumerable<T> orderedQuery;
            if (firstSpecification.OrderDirection == OrderDirection.Ascending)
            {
                orderedQuery = query.OrderBy(firstSpecification.KeySelector);
            }
            else
            {
                orderedQuery = query.OrderByDescending(firstSpecification.KeySelector);
            }

            foreach (var orderSpecification in orderSpecifications.Skip(1))
            {
                if (orderSpecification.OrderDirection == OrderDirection.Ascending)
                {
                    orderedQuery = orderedQuery.ThenBy(orderSpecification.KeySelector);
                }
                else
                {
                    orderedQuery = orderedQuery.ThenByDescending(orderSpecification.KeySelector);
                }
            }

            return orderedQuery.ToList();
        }
    }
}