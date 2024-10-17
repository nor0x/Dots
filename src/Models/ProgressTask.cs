using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dots.Models;

public partial class ProgressTask : ObservableObject
{
    public string Title { get; set; }
    public string Url { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public IProgress<(float progress, string task)> Progress { get; set; }

    [ObservableProperty]
    float? _value;

	[ObservableProperty]
	string? _task;
}
