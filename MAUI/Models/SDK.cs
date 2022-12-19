using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dots.Models;

public class SDK : ObservableObject
{
    //data
    public Version Version { get; set; }
    public string Appendix { get; set; }
    public string Path { get; set; }
    public string Link { get; set; }

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
