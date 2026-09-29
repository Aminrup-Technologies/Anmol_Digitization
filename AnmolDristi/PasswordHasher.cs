using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace AnmolDristi
{
    public class PasswordHasher
    {
        public const int SaltSize = 16;       // 128 bit
        public const int HashSize = 32;       // 256 bit
        public const int DefaultIterations = 100000;

        public static Tuple<string, string, int> CreateHash(string password, int iterations = DefaultIterations)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            using (var rng = new RNGCryptoServiceProvider())
            {
                var salt = new byte[SaltSize];
                rng.GetBytes(salt);
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    var hash = pbkdf2.GetBytes(HashSize);
                    return Tuple.Create(Convert.ToBase64String(hash), Convert.ToBase64String(salt), iterations);
                }
            }
        }

        public static bool Verify(string enteredPassword, string storedBase64Hash, string storedBase64Salt, int iterations)
        {
            if (enteredPassword == null) throw new ArgumentNullException(nameof(enteredPassword));
            if (string.IsNullOrEmpty(storedBase64Hash) || string.IsNullOrEmpty(storedBase64Salt))
                return false;

            byte[] salt;
            byte[] storedHash;
            try
            {
                salt = Convert.FromBase64String(storedBase64Salt);
                storedHash = Convert.FromBase64String(storedBase64Hash);
            }
            catch
            {
                return false;
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, iterations))
            {
                var testHash = pbkdf2.GetBytes(storedHash.Length);
                return AreEqualConstantTime(storedHash, testHash);
            }
        }

        private static bool AreEqualConstantTime(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}