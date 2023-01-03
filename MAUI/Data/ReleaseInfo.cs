using Microsoft.AppCenter.Analytics;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dots.Data;

public partial class ReleaseInfo
{
    [JsonPropertyName("channel-version")]
    public string ChannelVersion { get; set; }

    [JsonPropertyName("latest-release")]
    public string LatestRelease { get; set; }

    [JsonPropertyName("latest-release-date")]
    public DateTimeOffset LatestReleaseDate { get; set; }

    [JsonPropertyName("latest-runtime")]
    public string LatestRuntime { get; set; }

    [JsonPropertyName("latest-sdk")]
    public string LatestSdk { get; set; }

    [JsonPropertyName("release-type")]
    public string ReleaseType { get; set; }

    [JsonPropertyName("support-phase")]
    public string SupportPhase { get; set; }

    [JsonPropertyName("eol-date")]
    public DateTimeOffset EolDate { get; set; }

    [JsonPropertyName("lifecycle-policy")]
    public Uri LifecyclePolicy { get; set; }

    [JsonPropertyName("releases")]
    public Release[] Releases { get; set; }
}

public partial class Release
{
    [JsonPropertyName("release-date")]
    public DateTimeOffset ReleaseDate { get; set; }

    [JsonPropertyName("release-version")]
    public string ReleaseVersion { get; set; }

    [JsonPropertyName("security")]
    public bool Security { get; set; }

    [JsonPropertyName("cve-list")]
    public CveList[] CveList { get; set; }

    [JsonPropertyName("release-notes")]
    public Uri ReleaseNotes { get; set; }

    [JsonPropertyName("runtime")]
    public Runtime Runtime { get; set; }

    [JsonPropertyName("sdk")]
    public SdkInfo Sdk { get; set; }

    [JsonPropertyName("sdks")]
    public SdkInfo[] Sdks { get; set; }

    [JsonPropertyName("aspnetcore-runtime")]
    public AspnetcoreRuntime AspnetcoreRuntime { get; set; }

    [JsonPropertyName("windowsdesktop")]
    public Windowsdesktop Windowsdesktop { get; set; }

    [JsonIgnore]
    public bool Preview => Sdk.Version.Contains("-");

    [JsonIgnore]
    public bool SupportPhase { get; set; }
}

public partial class AspnetcoreRuntime
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("version-display")]
    public string VersionDisplay { get; set; }

    [JsonPropertyName("version-aspnetcoremodule")]
    public string[] VersionAspnetcoremodule { get; set; }

    [JsonPropertyName("vs-version")]
    public string VsVersion { get; set; }

    [JsonPropertyName("files")]
    public FileInfo[] Files { get; set; }
}

public partial class FileInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rid")]
    public Rid Rid { get; set; }

    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("akams")]
    public Uri Akams { get; set; }
}

public partial class CveList
{
    [JsonPropertyName("cve-id")]
    public string CveId { get; set; }

    [JsonPropertyName("cve-url")]
    public Uri CveUrl { get; set; }
}

public partial class Runtime
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("version-display")]
    public string VersionDisplay { get; set; }

    [JsonPropertyName("vs-version")]
    public string VsVersion { get; set; }

    [JsonPropertyName("vs-mac-version")]
    public string VsMacVersion { get; set; }

    [JsonPropertyName("files")]
    public FileInfo[] Files { get; set; }
}

public partial class SdkInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("version-display")]
    public string VersionDisplay { get; set; }

    [JsonPropertyName("runtime-version")]
    public string RuntimeVersion { get; set; }

    [JsonPropertyName("vs-version")]
    public string VsVersion { get; set; }

    [JsonPropertyName("vs-mac-version")]
    public string VsMacVersion { get; set; }

    [JsonPropertyName("vs-support")]
    public string VsSupport { get; set; }

    [JsonPropertyName("vs-mac-support")]
    public string VsMacSupport { get; set; }

    [JsonPropertyName("csharp-version")]
    public string CsharpVersion { get; set; }

    [JsonPropertyName("fsharp-version")]
    public string FsharpVersion { get; set; }

    [JsonPropertyName("vb-version")]
    public string VbVersion { get; set; }

    [JsonPropertyName("files")]
    public FileInfo[] Files { get; set; }
}

public partial class Windowsdesktop
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("version-display")]
    public string VersionDisplay { get; set; }

    [JsonPropertyName("files")]
    public FileInfo[] Files { get; set; }
}

public enum Rid { Empty, LinuxArm, LinuxArm64, LinuxMusl, LinuxMuslArm, LinuxMuslArm64, LinuxMuslX64, LinuxX64, OsxArm64, OsxX64, WinArm, WinArm64, WinX64, WinX86, WinX86X64, RhelX64, Rhel6X64, Fedora27X64, Fedora28X64, CentosX64, Debian9X64, OpenSuse423X64, UbuntuX64, Ubuntu1604X64, Ubuntu1804X64, DebianX64, Fedora27, Fedora28, Opensuse423, Ubuntu1604, Ubuntu1804, Fedora24X64, Fedora23X64, Opensuse132X64, Ubuntu1610X64, Opensuse421X64, Ubuntu1610 };


public class RidEnumConverter : JsonConverter<Rid>
{
    public override Rid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException();

        var value = reader.GetString();
        if (value == "")
            return Rid.Empty;

        return value switch
        {
            "linux-arm" => Rid.LinuxArm,
            "linux-arm64" => Rid.LinuxArm64,
            "linux-musl" => Rid.LinuxMusl,
            "linux-musl-arm" => Rid.LinuxMuslArm,
            "linux-musl-arm64" => Rid.LinuxMuslArm64,
            "linux-musl-x64" => Rid.LinuxMuslX64,
            "linux-x64" => Rid.LinuxX64,
            "osx-arm64" => Rid.OsxArm64,
            "osx-x64" => Rid.OsxX64,
            "win-arm" => Rid.WinArm,
            "win-arm64" => Rid.WinArm64,
            "win-x64" => Rid.WinX64,
            "win-x86" => Rid.WinX86,
            "rhel.6-x64" => Rid.Rhel6X64,
            "rhel-x64" => Rid.RhelX64,
            "win-x86_x64" => Rid.WinX86X64,
            "fedora.27-x64" => Rid.Fedora27X64,
            "fedora.28-x64" => Rid.Fedora28X64,
            "centos-x64" => Rid.Fedora27X64,
            "debian.9-x64" => Rid.Fedora27X64,
            "opensuse.42.3-x64" => Rid.OpenSuse423X64,
            "ubuntu-x64" => Rid.UbuntuX64,
            "ubuntu.16.04-x64" => Rid.Ubuntu1604X64,
            "ubuntu.18.04-x64" => Rid.Ubuntu1804X64,
            "debian-x64" => Rid.DebianX64,
            "fedora.27" => Rid.Fedora27,
            "fedora.28" => Rid.Fedora28,
            "opensuse.42.3" => Rid.Opensuse423,
            "ubuntu.16.04" => Rid.Ubuntu1604,
            "ubuntu.18.04" => Rid.Ubuntu1804,
            "fedora.24-x64" => Rid.Fedora24X64,
            "fedora.23-x64" => Rid.Fedora23X64,
            "opensuse.13.2-x64" => Rid.Opensuse132X64,
            "ubuntu.16.10-x64" => Rid.Ubuntu1610X64,
            "opensuse.42.1-x64" => Rid.Opensuse421X64,
            "ubuntu.16.10" => Rid.Ubuntu1610,
            "" => Rid.Empty,
            _ => PrintDiscard(value)
        };
    }




    Rid PrintDiscard(string v)
    {
        Analytics.TrackEvent("RID Discarded", new Dictionary<string, string> { { "value", v } });
        return Rid.Empty;
    }

    public override void Write(Utf8JsonWriter writer, Rid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            Rid.Empty => "",
            Rid.LinuxArm => "linux-arm",
            Rid.LinuxArm64 => "linux-arm64",
            Rid.LinuxMusl => "linux-musl",
            Rid.LinuxMuslArm => "linux-musl-arm",
            Rid.LinuxMuslArm64 => "linux-musl-arm64",
            Rid.LinuxMuslX64 => "linux-musl-x64",
            Rid.LinuxX64 => "linux-x64",
            Rid.OsxArm64 => "osx-arm64",
            Rid.OsxX64 => "osx-x64",
            Rid.WinArm => "win-arm",
            Rid.WinArm64 => "win-arm64",
            Rid.WinX64 => "win-x64",
            Rid.WinX86 => "win-x86",
            Rid.Rhel6X64 => "rhel.6-x64",
            Rid.RhelX64 => "rhel-x64",
            Rid.WinX86X64 => "win-x86_x64",
            Rid.Fedora27X64 => "fedora.27-x64",
            Rid.Fedora28X64 => "fedora.28-x64",
            Rid.CentosX64 => "centos-x64",
            Rid.Debian9X64 => "debian.9-x64",
            Rid.OpenSuse423X64 => "opensuse.42.3-x64",
            Rid.UbuntuX64 => "UbuntuX64",
            Rid.Ubuntu1604X64 => "ubuntu.16.04-x64",
            Rid.Ubuntu1804X64 => "ubuntu.18.04-x64",
            Rid.DebianX64 => "debian-x64",
            Rid.Fedora27 => "fedora.27",
            Rid.Fedora28 => "fedora.28",
            Rid.Opensuse423 => "opensuse.42.3",
            Rid.Ubuntu1604 => "ubuntu.16.04",
            Rid.Ubuntu1804 => "ubuntu.18.04",
            Rid.Fedora24X64 => "fedora.24-x64",
            Rid.Fedora23X64 => "fedora.23-x64",
            Rid.Opensuse132X64 => "opensuse.13.2-x64",
            Rid.Ubuntu1610X64 => "ubuntu.16.10-x64",
            Rid.Opensuse421X64 => "opensuse.42.1-x64",
            Rid.Ubuntu1610 => "ubuntu.16.10",
            _ => throw new NotSupportedException(),
        });
    }
}