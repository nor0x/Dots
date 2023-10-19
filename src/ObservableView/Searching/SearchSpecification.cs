using System;
using System.Linq;
using System.Linq.Expressions;

using ObservableView.Extensions;
using ObservableView.Filtering;
using ObservableView.Searching.Operands;
using ObservableView.Searching.Operations;
using ObservableView.Searching.Operators;
using System.Reflection;

using ObservableView.Searching.Processors;

namespace ObservableView.Searching
{
    public class SearchSpecification<T> : ISearchSpecification<T>
    {
        private const string DefaultSearchTextVariableName = "searchText";

        public event EventHandler SearchSpecificationAdded;

        public event EventHandler SearchSpecificationsCleared;

        public Operation BaseOperation { get; private set; }

        private static void EnsureOperator(Type propertyType, ref BinaryOperator @operator)
        {
            if (@operator == null)
            {
                // TODO: BinaryOperator @operator = null then take default depending on property type
                if (propertyType == typeof(string))
                {
                    @operator = BinaryOperator.Contains;
                }
                else if (propertyType == typeof(int))
                {
                    @operator = BinaryOperator.Equal;
                }
            }
        }

        public ISearchSpecification<T> Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, BinaryOperator @operator = null)
        {
            return this.Add(propertyExpression, null, @operator);
        }

        public ISearchSpecification<T> Add<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IExpressionProcessor[] expressionProcessors, BinaryOperator @operator = null)
        {
            if (propertyExpression == null)
            {
                throw new ArgumentNullException("propertyExpression");
            }

            var propertyInfo = ReflectionHelper<T>.GetProperty(propertyExpression);

            EnsureOperator(propertyInfo.PropertyType, ref @operator);

            if (this.BaseOperation == null)
            {
                this.BaseOperation = new BinaryOperation(@operator, new PropertyOperand(propertyInfo, expressionProcessors), new VariableOperand(DefaultSearchTextVariableName, propertyInfo.PropertyType));

                this.OnSearchSpecificationAdded();
            }
            else
            {
                return this.Or(propertyExpression, expressionProcessors, @operator);
            }

            return this;
        }

        public ISearchSpecification<T> And<TProperty>(Expression<Func<T, TProperty>> propertyExpression, BinaryOperator @operator = null)
        {
            return this.And(propertyExpression, null, @operator);
        }

        public ISearchSpecification<T> And<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IExpressionProcessor[] expressionProcessors, BinaryOperator @operator = null)
        {
            return this.CreateNestedOperation(propertyExpression, GroupOperator.And, @operator, expressionProcessors);
        }

        public ISearchSpecification<T> Or<TProperty>(Expression<Func<T, TProperty>> propertyExpression, BinaryOperator @operator = null)
        {
            return this.Or(propertyExpression, null, @operator);
        }

        public ISearchSpecification<T> Or<TProperty>(Expression<Func<T, TProperty>> propertyExpression, IExpressionProcessor[] expressionProcessors, BinaryOperator @operator = null)
        {
            return this.CreateNestedOperation(propertyExpression, GroupOperator.Or, @operator, expressionProcessors);
        }

        private ISearchSpecification<T> CreateNestedOperation<TProperty>(Expression<Func<T, TProperty>> propertyExpression, GroupOperator groupOperator, BinaryOperator @operator = null, IExpressionProcessor[] expressionProcessors = null)
        {
            if (this.BaseOperation == null)
            {
                throw new InvalidOperationException("Call Add beforehand.");
            }

            if (propertyExpression == null)
            {
                throw new ArgumentNullException("propertyExpression");
            }

            var propertyInfo = ReflectionHelper<T>.GetProperty(propertyExpression);

            EnsureOperator(propertyInfo.PropertyType, ref @operator);

            var nestedBinaryOperation = new BinaryOperation(@operator, new PropertyOperand(propertyInfo, expressionProcessors), new VariableOperand(DefaultSearchTextVariableName, propertyInfo.PropertyType));

            this.BaseOperation = new GroupOperation(this.BaseOperation, nestedBinaryOperation, groupOperator);

            this.OnSearchSpecificationAdded();
            return this;
        }

        private void OnSearchSpecificationAdded()
        {
            var handler = this.SearchSpecificationAdded;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void ReplaceSearchTextVariables<TX>(TX value)
        {
            this.ReplaceVariables(DefaultSearchTextVariableName, value);
        }

        private void ReplaceVariables<TX>(string variableName, TX value)
        {
            var variableOperands = this.BaseOperation.Flatten().OfType<VariableOperand>().Where(v => v.VariableName == variableName);

            foreach (var variableOperand in variableOperands)
            {
                variableOperand.Value = value;
            }
        }

        public void Clear()
        {
            this.BaseOperation = null;
            this.OnSearchSpecificationsCleared();
        }

        private void OnSearchSpecificationsCleared()
        {
            var handler = this.SearchSpecificationsCleared;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public bool Any()
        {
            return this.BaseOperation != null;
        }
    }
}