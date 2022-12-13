using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dots.Models
{
    public record SDK
    {
        public Version Version { get; init; }
        public string Path { get; init; }
        public bool Installed => !string.IsNullOrEmpty(Path);
        public string Link { get; init; }
        public string VersionText => Version.ToString();
    }
}
