namespace AuthService.Models;

/// <summary>What a client sends to log in.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>What a client sends to register a new customer.</summary>
public record RegisterRequest(string Username, string FullName, string Password, string Role = "Customer");
