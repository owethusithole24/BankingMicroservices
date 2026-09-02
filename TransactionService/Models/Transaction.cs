namespace TransactionService.Models;

/// <summary>
/// A record of an attempted money transfer. Owned exclusively by the Transaction
/// Service (database-per-service). Note there are NO account balances here — the
/// Transaction Service records that a transfer happened; the Account Service owns
/// the actual balances and does the money movement.
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public string FromAccountNumber { get; set; } = string.Empty;
    public string ToAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;

    /// <summary>"Pending", "Completed" or "Failed".</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Why it failed, if it did (e.g. "Insufficient funds").</summary>
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
