using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;

namespace Dots;

public class Constants
{
    public const string AppName = "Dots";
    public const string InstalledSdksKey = "installed-sdks-key";
    public const string LastCheckedKey = "last-checked";
    // throttles the app self-update check - separate from LastCheckedKey, which throttles the SDK release index
    public const string LastUpdateCheckKey = "last-update-check";
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
#elif LINUX
    // per-user SDK root. Installing here needs no root, works the same on every distro, and keeps
    // Dots away from distro-packaged SDKs under /usr - DotnetService.Uninstall refuses to touch those.
    public static readonly string DotnetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh";
    // .NET 7 dropped multi-level lookup: `dotnet --list-sdks` only reports what sits next to the host
    // it was invoked on. Prefer our own root and fall back to whatever the distro put on PATH.
    public static readonly string DotnetCommand = File.Exists(Path.Combine(DotnetRoot, "dotnet"))
        ? Path.Combine(DotnetRoot, "dotnet")
        : "dotnet";
    public const string ExplorerCommand = "xdg-open";
#else
    public const string UninstallerPath = "Package Cache";
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1";
    public const string DotnetCommand = "dotnet";
    public const string ExplorerCommand = "explorer";
#endif

    // derived so it can never drift from the script actually being downloaded
    public static readonly string InstallerScriptFileName = InstallerScript.Split('/')[^1];

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

	public const string CleanupText = "These SDKs are have newer versions installed or are EOL. Do you want to remove them?";
	public const string UpdateText = "These are the latest SDKs available that have an active support lifecycle. Do you want to update them?";
	public const string UpdateButtonText = "Update";
	public const string CleanupButtonText = "Cleanup";
	public const string CancelButtonText = "Cancel";

	public static IBrush CleanupBrush = new SolidColorBrush(Color.Parse("#bf8700"));
	public static IBrush UpdateBrush = new SolidColorBrush(Color.Parse("#2da44e"));
}
