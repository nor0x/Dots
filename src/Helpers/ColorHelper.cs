using System.Security.Cryptography;

namespace Dots.Helpers;

public class ColorHelper
{
    public static string GenerateHexColor(string input)
    {
        input = new string(input.ToArray().Reverse().ToArray());
        // Convert the input string to a byte array
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);

        // Use a cryptographic hash function to generate a hash from the input byte array
        byte[] hashBytes;
        using (var hashAlgorithm = SHA1.Create())
        {
            hashBytes = hashAlgorithm.ComputeHash(inputBytes);
        }

        // Take the first three bytes of the hash and convert them to hexadecimal
        string hexColor = "#" + BitConverter.ToString(hashBytes, 0, 3).Replace("-", "");

        return hexColor;
    }
}