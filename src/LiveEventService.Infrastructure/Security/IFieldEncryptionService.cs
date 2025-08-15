// SPDX-License-Identifier: MIT
namespace LiveEventService.Infrastructure.Security;

/// <summary>
/// Provides tolerant encryption and decryption for nullable string fields
/// to support at-rest protection of PII with graceful handling when keys are absent.
/// </summary>
public interface IFieldEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string when configured, or returns the input when null/empty or keys are missing.
    /// </summary>
    /// <param name="plaintext">The plaintext value.</param>
    /// <returns>Encrypted ciphertext, or original value when not applicable.</returns>
    string? EncryptNullable(string? plaintext);

    /// <summary>
    /// Decrypts a ciphertext string when configured, or returns the input when null/empty or keys are missing.
    /// </summary>
    /// <param name="ciphertext">The ciphertext value.</param>
    /// <returns>Decrypted plaintext, or original value when not applicable.</returns>
    string? DecryptNullable(string? ciphertext);
}
