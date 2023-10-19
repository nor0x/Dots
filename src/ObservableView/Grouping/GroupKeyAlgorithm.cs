namespace ObservableView.Grouping
{
    public abstract class GroupKeyAlgorithm<T> : IGroupKeyAlgorithm<T>, IGroupKeyAlgorithm
    {
        public string GetGroupKey(object item)
        {
            return this.GetGroupKey((T)item);
        }

        public abstract string GetGroupKey(T value);
    }
}