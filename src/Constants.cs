using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dots;

public class Constants
{
    public const string AppName = "Dots";
    public const string InstalledSdksKey = "installed-sdks-key";
    public const string LastCheckedKey = "last-checked";
#if MACOS
    public const string UninstallerPath = "Package Cache";
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh";
    public const string DotnetCommand = "/usr/local/share/dotnet/dotnet";
    public const string ExplorerCommand = "open";
    public const string UninstallScriptFile = """
                                                version="XXXXX"
                                                rm -rf /usr/local/share/dotnet/sdk/$version
                                                rm -rf /usr/local/share/dotnet/shared/Microsoft.NETCore.App/$version
                                                rm -rf /usr/local/share/dotnet/shared/Microsoft.AspNetCore.All/$version
                                                rm -rf /usr/local/share/dotnet/shared/Microsoft.AspNetCore.App/$version
                                                rm -rf /usr/local/share/dotnet/host/fxr/$version
                                            """;
#else
    public const string UninstallerPath = "Package Cache";
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1";
    public const string DotnetCommand = "dotnet";
    public const string ExplorerCommand = "explorer";
#endif

    public const string ListSdksCommand = "--list-sdks";
    public static string ReleaseInfoUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/";
    public static string ReleaseInfoUrlEnd = "/releases.json";
    public static string ReleaseIndexUrl = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json";
    public static string ReleaseIndexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName, "release-index.json");
    public static string ReleaseIndexKey = "release-index-key";
    public static string ReleaseBaseKey = "release-key-";
    public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    public const string SupportURl = "https://bento.me/nor0x";
    public const string GithubUrl = "https://github.com/nor0x/Dots";

    public const string DownloadingText = "Downloading...";
    public const string InstallingText = "Installing...";
    public const string UninstallingText = "Uninstalling...";
    public const string OpeningText = "Opening...";
}