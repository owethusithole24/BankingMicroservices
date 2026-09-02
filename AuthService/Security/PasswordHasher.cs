using System.Security.Cryptography;
using System.Text;

namespace AuthService.Security;

/// <summary>
/// Minimal password hashing for this student project: SHA-256 of the password.
/// (A production system would use a salted, slow hash such as BCrypt or PBKDF2 —
/// worth mentioning in your report, but SHA-256 keeps the demo self-contained.)
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
