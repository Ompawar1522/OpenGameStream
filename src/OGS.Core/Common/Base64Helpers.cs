using System.Text;

namespace OGS.Core.Common;

public static class Base64Helpers
{
    public static string EncodeBase64String(string input)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    }

    public static string DecodeBase64String(string input)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(input));
    }
}
