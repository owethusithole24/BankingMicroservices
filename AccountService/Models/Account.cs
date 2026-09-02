namespace AccountService.Models;

/// <summary>
/// A bank account owned by a customer. This is the core entity of the Account Service.
/// The Account Service owns this data exclusively (database-per-service pattern) —
/// no other service reads or writes the accounts table directly.
/// </summary>
public class Account
{
    public int Id { get; set; }

    /// <summary>Public account number shown to customers (e.g. 1000000001).</summary>
    public string AccountNumber { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    /// <summary>"Cheque" or "Savings".</summary>
    public string AccountType { get; set; } = "Cheque";

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "ZAR";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
