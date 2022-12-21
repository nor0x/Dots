using Dots.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dots.Models;

public partial class Sdk : ObservableObject
{
    [ObservableProperty]
    SdkInfo _data;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Installed))]
    string _path;

    //UI
    public string ColorHex { get; set; }
    public string Group => Data.Version.First().ToString();

    [JsonIgnore]
    public Color Color => Color.FromRgba(ColorHex);
    [JsonIgnore]
    public bool IsSelected { get; set; }
    [JsonIgnore]
    public bool Installed => !string.IsNullOrEmpty(_path);
}


public class InstalledSdk
{
    public string Version { get; set; }
    public string Path { get; set; }
}