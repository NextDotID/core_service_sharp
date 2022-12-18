namespace CoreService.Api.Injector;

using System.Security.Cryptography;
using System.Text;

public static class Generator
{
    private static readonly char[] AlphaNumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public static string AlphaNumeric(int length)
    {
        Span<byte> data = stackalloc byte[4 * length];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }

        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            var rnd = BitConverter.ToUInt32(data.Slice(i * 4, 4));
            var idx = rnd % AlphaNumericChars.Length;

            result.Append(AlphaNumericChars[idx]);
        }

        return result.ToString();
    }

    public static string Base64(int length)
    {
        Span<byte> data = stackalloc byte[length * 4 / 3];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }

        return Convert.ToBase64String(data)[..length];
    }
}
