namespace AuthService.Models;

/// <summary>
/// A bank customer / user who can log in. Owned exclusively by the Auth Service
/// (database-per-service) — the Account Service never sees this table.
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the password. We never store the raw password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"Admin" or "Customer" — used to restrict access to certain endpoints.</summary>
    public string Role { get; set; } = "Customer";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
