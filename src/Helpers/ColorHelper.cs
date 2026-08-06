using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

namespace Dots.Helpers;

public class ColorHelper
{
    // golden angle - consecutive numbers land far apart on the color wheel
    const double HueStep = 137.508;
    const double Saturation = 0.62;
    const double Lightness = 0.58;

    public static string GenerateHexColor(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            input = "0";
        }

        double hue;
        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            // numeric groups (major versions) get an evenly spread hue so
            // neighbours like 1, 10 and 11 never share a color
            hue = number * HueStep % 360d;
        }
        else
        {
            byte[] hashBytes;
            using (var hashAlgorithm = SHA1.Create())
            {
                hashBytes = hashAlgorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            }

            hue = (hashBytes[0] << 8 | hashBytes[1]) / 65535d * 360d;
        }

        return FromHsl(hue, Saturation, Lightness);
    }

    static string FromHsl(double hue, double saturation, double lightness)
    {
        var c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = c * (1 - Math.Abs(hue / 60d % 2 - 1));
        var m = lightness - c / 2;

        var (r, g, b) = (hue / 60d) switch
        {
            < 1 => (c, x, 0d),
            < 2 => (x, c, 0d),
            < 3 => (0d, c, x),
            < 4 => (0d, x, c),
            < 5 => (x, 0d, c),
            _ => (c, 0d, x),
        };

        return "#"
            + ToChannel(r + m)
            + ToChannel(g + m)
            + ToChannel(b + m);
    }

    static string ToChannel(double value) =>
        ((byte)Math.Clamp(Math.Round(value * 255), 0, 255)).ToString("X2");
}
