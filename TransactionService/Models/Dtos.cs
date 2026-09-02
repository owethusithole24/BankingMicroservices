namespace TransactionService.Models;

/// <summary>What a client sends to start a transfer.</summary>
public record CreateTransactionRequest(
    string FromAccountNumber,
    string ToAccountNumber,
    decimal Amount,
    string Reference = "");

/// <summary>The body we send to the Account Service to move the money.</summary>
public record TransferRequest(string FromAccountNumber, string ToAccountNumber, decimal Amount);

/// <summary>Outcome of the inter-service call to the Account Service.</summary>
public record TransferResult(bool Success, string? Error);
