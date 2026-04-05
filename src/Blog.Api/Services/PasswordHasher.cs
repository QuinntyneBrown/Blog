using System.Security.Cryptography;

namespace Blog.Api.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const char Delimiter = ':';
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);
        return string.Join(Delimiter,
            Convert.ToBase64String(salt),
            Iterations,
            Convert.ToBase64String(key));
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(Delimiter);
        if (parts.Length != 3) return false;

        var salt = Convert.FromBase64String(parts[0]);
        if (!int.TryParse(parts[1], out var iterations)) return false;
        var key = Convert.FromBase64String(parts[2]);
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, KeySize);
        return CryptographicOperations.FixedTimeEquals(derivedKey, key);
    }
}
