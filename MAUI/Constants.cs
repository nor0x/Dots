using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dots;

public class Constants
{
    public const string AppName = "Dots";
    public const string InstalledSdkSKey = "Installed-Sdks-Key";
#if MACCATALYST
    public const string UninstallerPath = "Package Cache";
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh";
#else 
    public const string UninstallerPath = "Package Cache";
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1";
#endif

    public static string ReleaseInfoUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/";
    public static string ReleaseInfoUrlEnd = "/releases.json";
    public static string ReleaseIndexUrl = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json";
}
