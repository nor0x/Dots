using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dots.Data;

public class ReleaseIndexInfo
{
    [JsonPropertyName("releases-index")]
    public ReleaseIndex[] ReleasesIndex { get; set; }
}

public class ReleaseIndex
{
    [JsonPropertyName("channel-version")]
    public string ChannelVersion { get; set; }

    [JsonPropertyName("latest-release")]
    public string LatestRelease { get; set; }

    [JsonPropertyName("latest-release-date")]
    public DateTimeOffset LatestReleaseDate { get; set; }

    [JsonPropertyName("security")]
    public bool Security { get; set; }

    [JsonPropertyName("latest-runtime")]
    public string LatestRuntime { get; set; }

    [JsonPropertyName("latest-sdk")]
    public string LatestSdk { get; set; }

    [JsonPropertyName("product")]
    public Product Product { get; set; }

    [JsonPropertyName("release-type")]
    public ReleaseType ReleaseType { get; set; }

    [JsonPropertyName("support-phase")]
    public SupportPhase SupportPhase { get; set; }

    [JsonPropertyName("eol-date")]
    public DateTimeOffset? EolDate { get; set; }

    [JsonPropertyName("releases.json")]
    public Uri ReleasesJson { get; set; }
}

public enum Product { Net, NetCore, Undefined };

public enum ReleaseType { Lts, Sts, Undefined };

public enum SupportPhase { Active, Eol, Preview, Undefined };


public class ProductEnumConverter : JsonConverter<Product>
{
    public override Product Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            ".NET" => Product.Net,
            ".NET Core" => Product.NetCore,
            _ => Product.Undefined
        };
    }

    public override void Write(Utf8JsonWriter writer, Product value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            Product.Net => ".NET",
            Product.NetCore => ".NET Core",
            _ => ""
        });
    }
}

public class ReleaseTypeEnumConverter : JsonConverter<ReleaseType>
{
    public override ReleaseType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "lts" => ReleaseType.Lts,
            "sts" => ReleaseType.Sts,
            _ => ReleaseType.Undefined
        };
    }

    public override void Write(Utf8JsonWriter writer, ReleaseType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            ReleaseType.Lts => "lts",
            ReleaseType.Sts => "sts",
            _ => ""
        });
    }
}

public class SupportPhaseEnumConverter : JsonConverter<SupportPhase>
{
    public override SupportPhase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "preview" => SupportPhase.Preview,
            "active" => SupportPhase.Active,
            "eol" => SupportPhase.Eol,
            _ => SupportPhase.Undefined
        };
    }

    public override void Write(Utf8JsonWriter writer, SupportPhase value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            SupportPhase.Preview => "preview",
            SupportPhase.Active => "active",
            SupportPhase.Eol => "eol",
            _ => ""
        });
    }
}

public class ReleaseSerializerOptions
{
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new ProductEnumConverter(),
            new RidEnumConverter(),
            new ReleaseTypeEnumConverter(),
            new SupportPhaseEnumConverter()
        }
    };
}