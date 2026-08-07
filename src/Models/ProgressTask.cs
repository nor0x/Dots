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

	/// <summary>
	/// True while the server sends no Content-Length, so the bar animates instead of sitting at 0.
	/// </summary>
	[ObservableProperty]
	bool _isIndeterminate;

	/// <summary>
	/// False once an installer process is running. Killing a burn bundle or msiexec mid-transaction
	/// can leave a half-installed SDK and a stuck _MSIExecute mutex, so the cancel button is disabled
	/// there rather than being wired up to do something dangerous.
	/// </summary>
	[ObservableProperty]
	bool _canCancel = true;
}
