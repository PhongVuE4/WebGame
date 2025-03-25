using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TruthOrDare_Contract.IServices;

namespace TruthOrDare_Core.Services
{
    public class PasswordHashingService : IPasswordHashingService
    {
        public byte[] GenerateSalt()
        {
            byte[] salf = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salf);
            }
            return salf;
        }

        private byte[] ComputeHash(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] passwordWithSalfBytes = new byte[passwordBytes.Length + salt.Length];

                Buffer.BlockCopy(passwordBytes, 0, passwordWithSalfBytes, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, passwordWithSalfBytes, passwordBytes.Length, salt.Length);

                return sha256.ComputeHash(passwordWithSalfBytes);
            }
        }

        public string HashPassword(string password, byte[] salf)
        {
            byte[] hash = ComputeHash(password, salf);

            string salfBase64 = Convert.ToBase64String(salf);
            string hashBase64 = Convert.ToBase64String(hash);
            string hashedPassword = $"{salfBase64}:{hashBase64}";

            return hashedPassword;
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            string[] parts = hashedPassword.Split(':');
            string salfBase64 = parts[0];
            string hashBase64 = parts[1];

            byte[] salf = Convert.FromBase64String(salfBase64);
            byte[] hash = Convert.FromBase64String(hashBase64);

            byte[] computeHash = ComputeHash(password, salf);
            return CompareHashes(hash, computeHash);
        }

        private bool CompareHashes(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length)
            {
                return false;
            }

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
