using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dots;

public class Constants
{
    public const string AppName = "Dots";
    public const string InstalledSDKSKey = "Installed-SDKS";
#if MACCATALYST
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh";
#else 
    public const string UninstallerPath = "Package Cache";
    public const string InstallerScript = "https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1";
#endif
}
