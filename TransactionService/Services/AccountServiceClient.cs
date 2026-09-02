using System.Net.Http.Json;
using TransactionService.Models;

namespace TransactionService.Services;

/// <summary>
/// Typed HttpClient that calls the Account Service over HTTP. This is the
/// inter-service communication: the Transaction Service does NOT touch the
/// accounts database — it asks the Account Service to move the money.
/// The caller's JWT is forwarded so the security context travels with the call.
/// </summary>
public class AccountServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AccountServiceClient> _logger;

    public AccountServiceClient(HttpClient http, ILogger<AccountServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TransferResult> TransferAsync(TransferRequest request, string? authorizationHeader)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/transfer")
        {
            Content = JsonContent.Create(request)
        };

        // Forward the caller's bearer token so the Account Service can authorise the call.
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
            message.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

        try
        {
            var response = await _http.SendAsync(message);
            if (response.IsSuccessStatusCode)
                return new TransferResult(true, null);

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Account Service rejected transfer: {Status} {Body}",
                (int)response.StatusCode, body);
            return new TransferResult(false, $"Account Service returned {(int)response.StatusCode}: {body}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not reach the Account Service");
            return new TransferResult(false, "Account Service unreachable");
        }
    }
}
