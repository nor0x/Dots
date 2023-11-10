using Avalonia.Media;
using Dots.Data;
using System.Text.Json.Serialization;

namespace Dots.Models;

public partial class Sdk : ObservableObject
{
    [ObservableProperty]
    Release _data;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Installed))]
    string _path = "";

    //UI
    public string ColorHex { get; set; }
    public string Group => Data.Sdk.Version.First().ToString();

    [JsonIgnore]
    public IBrush Color => SolidColorBrush.Parse(ColorHex);

    [JsonIgnore]
    public bool IsSelected { get; set; }
    [JsonIgnore]
    public bool Installed => !string.IsNullOrEmpty(Path);

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
    public double _progress;

    [JsonIgnore]
    public bool IsBusy => _isDownloading || _isInstalling;

    [JsonIgnore]
    public string VersionDisplay { get; set; }

    [JsonIgnore]
    public ProgressTask ProgressTask { get; set; }
}


public class InstalledSdk
{
    public string Version { get; set; }
    public string Path { get; set; }
}