using System;

namespace ObservableView.Exceptions
{
    public class SearchTextPreprocessorException : Exception
    {
        public SearchTextPreprocessorException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}