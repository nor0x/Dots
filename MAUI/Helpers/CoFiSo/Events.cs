using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoFiSo;

public delegate void CurrentChangingEventHandler(object sender, CurrentChangingEventArgs e);


public partial class CurrentChangingEventArgs
{
    private bool _cancel;
    public bool Cancel
    {
        get => _cancel;
        set
        {
            if (!IsCancelable)
            {
                throw new InvalidOperationException();
            }
            _cancel = value;
        }
    }

    public bool IsCancelable { get; } = true;
    public CurrentChangingEventArgs() { }
    public CurrentChangingEventArgs(bool isCancelable)
    {
        IsCancelable = isCancelable;
    }
}