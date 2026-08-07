using Avalonia.Media;
using Dots.Data;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Dots.Models;

[DebuggerDisplay("{VersionDisplay}")]
public partial class Sdk : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ShowSupportPhase))]
	Release _data;

	[ObservableProperty]
	SdkInfo _sdkData;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Installed))]
	[NotifyPropertyChangedFor(nameof(IsExternallyManaged))]
	[NotifyPropertyChangedFor(nameof(CanInstallOrUninstall))]
	string _path = "";

	/// <summary>
	/// Who owns this install when it isn't Dots - "Visual Studio" for an SDK the VS installer put
	/// there, for instance. Null when Dots can manage it, or on platforms with no such concept.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsExternallyManaged))]
	[NotifyPropertyChangedFor(nameof(CanInstallOrUninstall))]
	[NotifyPropertyChangedFor(nameof(ExternalOwnerLabel))]
	string? _externalOwner;

	/// <summary>Why Dots can't remove it, phrased for the user. Shown as the button's tooltip.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsExternallyManaged))]
	[NotifyPropertyChangedFor(nameof(CanInstallOrUninstall))]
	string? _uninstallBlockedReason;

	//UI
	public string ColorHex { get; set; }
	public int Group => Convert.ToInt16(VersionDisplay?.Split('.').FirstOrDefault());

	[JsonIgnore]
	public IBrush Color => SolidColorBrush.Parse(ColorHex);

	[JsonIgnore]
	public bool IsSelected { get; set; }
	[JsonIgnore]
	public bool Installed => !string.IsNullOrEmpty(Path);

	/// <summary>Installed, but by something other than Dots - drives the pill in the list.</summary>
	[JsonIgnore]
	public bool IsExternallyManaged => Installed && !string.IsNullOrEmpty(UninstallBlockedReason);

	/// <summary>
	/// False only for an installed SDK Dots can't remove. A not-yet-installed SDK is always
	/// actionable - the button says "Install" there, and nothing blocks installing.
	/// </summary>
	[JsonIgnore]
	public bool CanInstallOrUninstall => !IsExternallyManaged;

	/// <summary>Pill text - the owner when known, otherwise a generic marker.</summary>
	[JsonIgnore]
	public string ExternalOwnerLabel => string.IsNullOrEmpty(ExternalOwner) ? "Not removable" : ExternalOwner;

	// the preview pill already says "Preview"/"Release Candidate" for these,
	// so the support phase pill would just repeat it
	[JsonIgnore]
	public bool ShowSupportPhase => Data is not { Preview: true, SupportPhase: SupportPhase.Preview };

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsBusy))]
	[JsonIgnore]
	public bool _isDownloading;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsBusy))]
	[JsonIgnore]
	public bool _isInstalling;

	[ObservableProperty]
	[JsonIgnore]
	public string _statusMessage;

	[ObservableProperty]
	[JsonIgnore]
	double _progress;

	[JsonIgnore]
	public bool IsBusy => _isDownloading || _isInstalling;

	[JsonIgnore]
	public string VersionDisplay { get; set; }

	[ObservableProperty]
	[JsonIgnore]
	ProgressTask _progressTask;
}


public class InstalledSdk
{
	public string Version { get; set; }
	public string Path { get; set; }
}
