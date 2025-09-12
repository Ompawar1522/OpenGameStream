using System.Security.Cryptography;
using System.Text;

namespace OGS.Core.Common;

public sealed class AesContext
{
    public byte[] Secret { get; } 
    public string SecretBase64 => Convert.ToBase64String(Secret);

    private AesContext(byte[] key)
    {
        Secret = key;
    }

    public static AesContext FromKey(byte[] key) => new AesContext(key);
    public static AesContext FromBase64Key(string base64Key) => new AesContext(Convert.FromBase64String(base64Key));
    
    public static AesContext GenerateRandom()
    {
        byte[] key = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }

        return new AesContext(key);
    }
    
    public byte[] Decrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> tag)
    {
        using (AesGcm aes = new AesGcm(Secret, 16))
        {
            byte[] buffer = new byte[input.Length];

            aes.Decrypt(iv, input, tag, buffer);
            return buffer;
        }
    }

    public byte[] Encrypt(ReadOnlySpan<byte> input, out byte[] iv, out byte[] tag)
    {
        using (AesGcm aes = new AesGcm(Secret, 16))
        {
            iv = new byte[12];
            RandomNumberGenerator.Fill(iv);

            byte[] ciphertext = new byte[input.Length];
            tag = new byte[16];

            aes.Encrypt(iv, input, ciphertext, tag);
            return ciphertext;
        }
    }

    public string EncryptStringToBase64String(string input, out string iv, out string tag)
    {
        byte[] data = Encrypt(Encoding.UTF8.GetBytes(input), out byte[] ivBytes, out byte[] tagBytes);

        iv = Convert.ToBase64String(ivBytes);
        tag = Convert.ToBase64String(tagBytes);
        return Convert.ToBase64String(data);
    }

    public string DecryptStringFromBase64String(string input, string iv, string tag)
    {
        byte[] inputBytes = Convert.FromBase64String(input);
        byte[] ivBytes = Convert.FromBase64String(iv);
        byte[] tagBytes = Convert.FromBase64String(tag);

        byte[] output = Decrypt(inputBytes, ivBytes, tagBytes);

        return Encoding.UTF8.GetString(output);
    }
}