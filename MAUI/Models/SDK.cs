using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public Color Color { get; set; }
    public bool IsSelected { get; set; }
    public bool Installed => !string.IsNullOrEmpty(Path);
    public string VersionText => Version.ToString() + Appendix;
}
