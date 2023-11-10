using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dots.Models;

public class ProgressTask
{
    public string Url { get; set; }
    public int Progress { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
    public Task DownloadTask { get; set; }
}
