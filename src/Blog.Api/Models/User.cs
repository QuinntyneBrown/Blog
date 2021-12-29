using Blog.Api.Core;
using System;
using System.Security.Cryptography;

namespace Blog.Api.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public byte[] Salt { get; set; }

        public User(string username, string password, IPasswordHasher passwordHasher)
        {
            Salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(Salt);
            }
            Username = username;
            Password = passwordHasher.HashPassword(Salt, password);
        }

        private User()
        {

        }
    }
}
