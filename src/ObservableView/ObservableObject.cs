using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace ObservableView
{
    [Preserve(AllMembers = true)]
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Called when a property value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the tx.Generic type T.Generic type T.</typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        /// <exception cref="ArgumentException">
        ///     'propertyExpression' should be a member expression
        ///     or
        ///     'propertyExpression' body should be a constant expression.
        /// </exception>
        [Obsolete("Get rid of expression based property change notifications!")]
        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var body = propertyExpression.Body as MemberExpression;
                if (body == null)
                {
                    throw new ArgumentException("'propertyExpression' should be a member expression");
                }

                var expression = body.Expression as ConstantExpression;
                if (expression == null)
                {
                    throw new ArgumentException("'propertyExpression' body should be a constant expression");
                }

                var e = new PropertyChangedEventArgs(body.Member.Name);
                handler(this, e);
            }
        }
    }
}