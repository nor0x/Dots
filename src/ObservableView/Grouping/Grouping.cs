using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ObservableView.Grouping
{
    [DebuggerDisplay("Key = {Key}, Count = {this.Items.Count}")]
    public class Grouping<TV> : ObservableCollection<TV>
    {
        public string Key { get; private set; }

        public Grouping(string key, IEnumerable<TV> items)
        {
            this.Key = key;
            foreach (var item in items)
            {
                this.Items.Add(item);
            }
        }

        public override string ToString()
        {
            return string.Format("Key={0}, Count={1}", this.Key, this.Items.Count);
        }
    }
}