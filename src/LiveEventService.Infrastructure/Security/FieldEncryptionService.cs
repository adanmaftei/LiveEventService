// SPDX-License-Identifier: MIT
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace LiveEventService.Infrastructure.Security;

public sealed class FieldEncryptionService : IFieldEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public FieldEncryptionService(IConfiguration configuration)
    {
        // Expect base64 strings in configuration
        var keyB64 = configuration["Security:Encryption:Key"];
        var ivB64 = configuration["Security:Encryption:IV"];

        if (string.IsNullOrWhiteSpace(keyB64) || string.IsNullOrWhiteSpace(ivB64))
        {
            // Fallback to pass-through if not configured
            _key = Array.Empty<byte>();
            _iv = Array.Empty<byte>();
        }
        else
        {
            _key = Convert.FromBase64String(keyB64);
            _iv = Convert.FromBase64String(ivB64);
        }
    }

    public string? EncryptNullable(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;
        if (_key.Length == 0 || _iv.Length == 0) return plaintext; // pass-through when not configured

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipherBytes);
    }

    public string? DecryptNullable(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;
        if (_key.Length == 0 || _iv.Length == 0) return ciphertext; // pass-through when not configured

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(ciphertext);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return System.Text.Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If old records are in plaintext, return as-is
            return ciphertext;
        }
    }
}


