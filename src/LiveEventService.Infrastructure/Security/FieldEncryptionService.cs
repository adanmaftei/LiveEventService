// SPDX-License-Identifier: MIT
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace LiveEventService.Infrastructure.Security;

/// <summary>
/// Service for encrypting and decrypting sensitive field values using AES encryption.
/// </summary>
public sealed class FieldEncryptionService : IFieldEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldEncryptionService"/> class.
    /// Initializes the service from configuration values. Accepts base64-encoded keys
    /// or derives bytes deterministically from arbitrary secrets.
    /// </summary>
    /// <param name="configuration">The configuration containing encryption settings.</param>
    public FieldEncryptionService(IConfiguration configuration)
    {
        // Prefer base64-encoded key/iv; if not base64, derive bytes deterministically from provided strings
        var keyRaw = configuration["Security:Encryption:Key"];
        var ivRaw = configuration["Security:Encryption:IV"];

        if (string.IsNullOrWhiteSpace(keyRaw) || string.IsNullOrWhiteSpace(ivRaw))
        {
            // Not configured: pass-through
            _key = Array.Empty<byte>();
            _iv = Array.Empty<byte>();
            return;
        }

        // Try base64 first
        if (TryFromBase64(keyRaw, out var keyBytes) && TryFromBase64(ivRaw, out var ivBytes))
        {
            _key = keyBytes!;
            _iv = ivBytes!;
            return;
        }

        // Derive from arbitrary secrets: produce AES-256 key (32 bytes) and IV (16 bytes)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var keyHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keyRaw));
        var ivHash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("iv:" + ivRaw));
        _key = keyHash; // 32 bytes
        _iv = new byte[16];
        Array.Copy(ivHash, _iv, 16);
    }

    /// <summary>
    /// Attempts to parse a base64-encoded string into bytes.
    /// </summary>
    /// <param name="input">The base64 string to parse.</param>
    /// <param name="bytes">The parsed bytes, or null if parsing failed.</param>
    /// <returns>True if parsing was successful; otherwise false.</returns>
    private static bool TryFromBase64(string input, out byte[]? bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(input);
            return true;
        }
        catch
        {
            bytes = null;
            return false;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
