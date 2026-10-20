using System.Security.Cryptography;
using System.Text;

namespace SchoolMvc.Helpers;

public static class RSAHelper
{
    public static (string publicKey, string privateKey) GenerateKeys()
    {
        using var rsa = RSA.Create(2048);
        string publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        string privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        return (publicKey, privateKey);
    }

    public static string EncryptWithPublicKey(string text, string publicKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
        byte[] data = Encoding.UTF8.GetBytes(text);
        byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encrypted);
    }

    public static string DecryptWithPrivateKey(string encryptedText, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        byte[] data = Convert.FromBase64String(encryptedText);
        byte[] decrypted = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
        return Encoding.UTF8.GetString(decrypted);
    }
}