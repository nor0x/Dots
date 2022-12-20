using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dots.Models;

public partial class SDK : ObservableObject
{
    //data
    [ObservableProperty]
    Version _version;
    [ObservableProperty]
    string _appendix;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Installed))]
    string _path;
    [ObservableProperty]
    string _link;

    //UI
    public string ColorHex { get; set; }

    [JsonIgnore]
    public Color Color => Color.FromRgba(ColorHex);
    [JsonIgnore]
    public bool IsSelected { get; set; }
    [JsonIgnore]
    public bool Installed => !string.IsNullOrEmpty(Path);
    [JsonIgnore]
    public string VersionText => Version.ToString() + Appendix;
}
