using System.Security.Cryptography;
using System.Text;

namespace RealEstateCRM.Security
{
    public enum PasswordVerifyResult
    {
        Failed,
        Success,
        /// <summary>Password is correct but the stored hash is legacy/weak and should be replaced.</summary>
        SuccessRehashNeeded
    }

    /// <summary>
    /// PBKDF2-HMAC-SHA256 password hashing with a per-password random salt.
    /// Stored format: v1.{iterations}.{saltBase64}.{hashBase64}
    /// Legacy unsalted SHA-256 hex hashes (64 chars) are still accepted once and flagged for upgrade.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        public const int Iterations = 600_000;
        private const string Prefix = "v1";

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Prefix}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static PasswordVerifyResult Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return PasswordVerifyResult.Failed;

            if (stored.StartsWith(Prefix + ".", StringComparison.Ordinal))
            {
                var parts = stored.Split('.');
                if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations < 1)
                    return PasswordVerifyResult.Failed;

                try
                {
                    var salt = Convert.FromBase64String(parts[2]);
                    var expected = Convert.FromBase64String(parts[3]);
                    var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                    if (!CryptographicOperations.FixedTimeEquals(actual, expected))
                        return PasswordVerifyResult.Failed;

                    return iterations < Iterations ? PasswordVerifyResult.SuccessRehashNeeded : PasswordVerifyResult.Success;
                }
                catch (FormatException)
                {
                    return PasswordVerifyResult.Failed;
                }
            }

            // Legacy unsalted SHA-256 (hex). Accept once, then force an upgrade.
            if (stored.Length == 64)
            {
                var legacy = Encoding.UTF8.GetBytes(LegacySha256(password));
                var storedBytes = Encoding.UTF8.GetBytes(stored.ToLowerInvariant());
                return CryptographicOperations.FixedTimeEquals(legacy, storedBytes)
                    ? PasswordVerifyResult.SuccessRehashNeeded
                    : PasswordVerifyResult.Failed;
            }

            return PasswordVerifyResult.Failed;
        }

        public static string LegacySha256(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
