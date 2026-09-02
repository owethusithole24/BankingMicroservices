namespace AccountService.Models;

/// <summary>Body for POST /api/accounts/transfer — move money between two accounts.</summary>
public record TransferRequest(string FromAccountNumber, string ToAccountNumber, decimal Amount);
