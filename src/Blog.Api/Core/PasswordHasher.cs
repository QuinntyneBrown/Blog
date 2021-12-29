using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;

namespace Blog.Api.Core
{
    public interface IPasswordHasher
    {
        string HashPassword(byte[] salt, string password);
    }

    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(byte[] salt, string password)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA1,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        }
    }
}
