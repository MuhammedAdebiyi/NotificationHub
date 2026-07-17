using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var base64Key = configuration["DataSources:EncryptionKey"]
            ?? throw new InvalidOperationException("DataSources:EncryptionKey is not configured.");

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "DataSources:EncryptionKey must decode to exactly 32 bytes (AES-256).");
        }
    }

    public string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Layout: nonce || tag || ciphertext
        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertextB64)
    {
        var data = Convert.FromBase64String(ciphertextB64);

        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var ciphertext = data[(NonceSize + TagSize)..];
        var plaintextBytes = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_key, TagSize);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return System.Text.Encoding.UTF8.GetString(plaintextBytes);
    }
}