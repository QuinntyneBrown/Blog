using System.Security.Cryptography;

namespace Blog.Api.Services;

/// <summary>
/// PBKDF2-based password hasher whose stored format encodes the algorithm name so
/// that future algorithm upgrades can verify old hashes without a schema change.
///
/// Format (4 colon-delimited fields):
///   {algorithmName}:{base64Salt}:{iterations}:{base64DerivedKey}
///
/// Design reference: docs/detailed-designs/01-authentication/README.md,
/// Section 3.4 — PasswordHasher ("hash format encodes algorithm parameters so future
/// upgrades are backward-compatible") and Section 7.1 — Password Hashing.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const char Delimiter = ':';
    private static readonly HashAlgorithmName DefaultAlgorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, DefaultAlgorithm, KeySize);
        // Encode algorithm name as the first field so future migrations can dispatch
        // to the correct algorithm when verifying old hashes.
        return string.Join(Delimiter,
            DefaultAlgorithm.Name,
            Convert.ToBase64String(salt),
            Iterations,
            Convert.ToBase64String(key));
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(Delimiter);

        // Support both the new 4-field format and the legacy 3-field format
        // (salt:iterations:key) that was written before the algorithm field was added.
        // Legacy hashes are assumed to have been produced with SHA-256.
        HashAlgorithmName algorithm;
        string saltPart, iterationsPart, keyPart;

        if (parts.Length == 4)
        {
            algorithm = new HashAlgorithmName(parts[0]);
            saltPart = parts[1];
            iterationsPart = parts[2];
            keyPart = parts[3];
        }
        else if (parts.Length == 3)
        {
            // Legacy format: assume SHA-256 (the only algorithm ever used before this fix).
            algorithm = HashAlgorithmName.SHA256;
            saltPart = parts[0];
            iterationsPart = parts[1];
            keyPart = parts[2];
        }
        else
        {
            return false;
        }

        if (!int.TryParse(iterationsPart, out var iterations)) return false;

        byte[] salt, key;
        try
        {
            salt = Convert.FromBase64String(saltPart);
            key = Convert.FromBase64String(keyPart);
        }
        catch (FormatException)
        {
            return false;
        }

        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, KeySize);
        return CryptographicOperations.FixedTimeEquals(derivedKey, key);
    }
}
