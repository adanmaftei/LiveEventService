// SPDX-License-Identifier: MIT
namespace LiveEventService.Infrastructure.Security;

public interface IFieldEncryptionService
{
    string? EncryptNullable(string? plaintext);
    string? DecryptNullable(string? ciphertext);
}


