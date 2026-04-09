using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using UserService.Application.Interfaces;

namespace UserService.Application.Services;

/// <summary>
/// AES-GCM encryption service with backward compatibility for legacy AES-CBC ciphertexts.
///
/// FORMAT:
///   New (GCM):    Base64Url( [0x02] + nonce(12) + tag(16) + ciphertext )
///   Legacy (CBC): Base64Url( iv(16) + ciphertext )
///
/// MIGRATION NOTES:
///   Encrypt always produces GCM output (authenticated, nonce-based, no padding oracle risk).
///   Decrypt auto-detects the format by checking the first byte:
///     - 0x02  → AES-256-GCM with HKDF-derived key
///     - other → legacy AES-256-CBC with original PadRight(32) key (backward compat)
///
///   Client-side code that sends encrypted payloads (e.g. passwords) must be updated
///   to produce the GCM format. Until then, CBC payloads continue to decrypt correctly.
/// </summary>
public class EncryptionService : IEncryptionService
{
    // Version byte written at the start of every GCM ciphertext
    private const byte GcmVersion = 0x02;
    private const int GcmNonceSize = 12;   // 96-bit nonce — AES-GCM standard
    private const int GcmTagSize   = 16;   // 128-bit authentication tag

    // Legacy CBC constants (kept for decrypting existing data)
    private const int CbcIvLength  = 16;

    private readonly byte[] _gcmKey; // HKDF-derived 32-byte key for AES-256-GCM
    private readonly byte[] _cbcKey; // Original PadRight(32) key — legacy CBC compat only

    public EncryptionService(IConfiguration config)
    {
        var keyString = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured");

        // Legacy CBC key: exact same derivation as before so old ciphertexts still decrypt.
        _cbcKey = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));

        // GCM key: derive a proper 32-byte key using HKDF-SHA256 from the same master key.
        // Using a fixed info label ensures this subkey is purpose-bound.
        _gcmKey = HKDF.DeriveKey(
            hashAlgorithmName: HashAlgorithmName.SHA256,
            ikm: Encoding.UTF8.GetBytes(keyString),
            outputLength: 32,
            salt: null,
            info: Encoding.UTF8.GetBytes("UserService:AesGcm:v2"));
    }

    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce      = new byte[GcmNonceSize];
        var tag        = new byte[GcmTagSize];
        var cipher     = new byte[plainBytes.Length];

        RandomNumberGenerator.Fill(nonce);

        using var aesGcm = new AesGcm(_gcmKey, GcmTagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipher, tag);

        // Layout: [version(1)] + [nonce(12)] + [tag(16)] + [ciphertext]
        var output = new byte[1 + GcmNonceSize + GcmTagSize + cipher.Length];
        output[0] = GcmVersion;
        Buffer.BlockCopy(nonce,  0, output, 1,                               GcmNonceSize);
        Buffer.BlockCopy(tag,    0, output, 1 + GcmNonceSize,                GcmTagSize);
        Buffer.BlockCopy(cipher, 0, output, 1 + GcmNonceSize + GcmTagSize,  cipher.Length);

        return ToUrlSafeBase64(output);
    }

    public string Decrypt(string encryptedText)
    {
        var bytes = FromUrlSafeBase64(encryptedText);

        if (bytes.Length > 0 && bytes[0] == GcmVersion)
            return DecryptGcm(bytes);

        return DecryptLegacyCbc(bytes);
    }

    // -------------------------------------------------------------------------

    private string DecryptGcm(byte[] bytes)
    {
        var minLength = 1 + GcmNonceSize + GcmTagSize;
        if (bytes.Length < minLength)
            throw new CryptographicException("GCM ciphertext is too short.");

        var nonce  = bytes[1..(1 + GcmNonceSize)];
        var tag    = bytes[(1 + GcmNonceSize)..(1 + GcmNonceSize + GcmTagSize)];
        var cipher = bytes[(1 + GcmNonceSize + GcmTagSize)..];
        var plain  = new byte[cipher.Length];

        using var aesGcm = new AesGcm(_gcmKey, GcmTagSize);
        aesGcm.Decrypt(nonce, cipher, tag, plain);   // throws CryptographicException if tag invalid

        return Encoding.UTF8.GetString(plain);
    }

    private string DecryptLegacyCbc(byte[] bytes)
    {
        if (bytes.Length < CbcIvLength)
            throw new CryptographicException("CBC ciphertext is too short.");

        var iv     = bytes[..CbcIvLength];
        var cipher = bytes[CbcIvLength..];

        using var aes = Aes.Create();
        aes.Key     = _cbcKey;
        aes.IV      = iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }

    // -------------------------------------------------------------------------

    private static string ToUrlSafeBase64(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static byte[] FromUrlSafeBase64(string s)
    {
        var normalized = s.Replace('-', '+').Replace('_', '/');
        var padding    = (4 - normalized.Length % 4) % 4;
        normalized    += new string('=', padding);
        return Convert.FromBase64String(normalized);
    }
}
