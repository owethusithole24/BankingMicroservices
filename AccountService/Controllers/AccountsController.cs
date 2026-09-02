using AccountService.Data;
using AccountService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Controllers;

/// <summary>
/// REST endpoints for bank accounts. Follows REST conventions:
/// clear nouns, correct HTTP verbs, meaningful status codes, JSON bodies.
/// </summary>
[ApiController]
[Route("api/[controller]")] // -> /api/accounts
public class AccountsController : ControllerBase
{
    private readonly AccountDbContext _db;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(AccountDbContext db, ILogger<AccountsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>GET /api/accounts — list all accounts.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Account>>> GetAll()
    {
        _logger.LogInformation("Fetching all accounts");
        return Ok(await _db.Accounts.ToListAsync());
    }

    /// <summary>GET /api/accounts/{id} — fetch one account.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Account>> GetById(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null)
        {
            _logger.LogWarning("Account {AccountId} not found", id);
            return NotFound();
        }
        return Ok(account);
    }

    /// <summary>POST /api/accounts — open a new account. Requires a valid token.</summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Account>> Create(Account account)
    {
        account.Id = 0;                       // never trust a client-supplied id
        account.CreatedAt = DateTime.UtcNow;

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Opened account {AccountId} ({AccountNumber}) for {Owner}",
            account.Id, account.AccountNumber, account.OwnerName);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    /// <summary>
    /// POST /api/accounts/transfer — move money between two accounts by account number.
    /// Called by the Transaction Service (inter-service call). Requires a valid token.
    /// The debit and credit happen together in this one database, so balances stay consistent.
    /// </summary>
    [Authorize]
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferRequest req)
    {
        if (req.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero" });

        var from = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == req.FromAccountNumber);
        var to = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == req.ToAccountNumber);

        if (from is null || to is null)
            return NotFound(new { message = "Source or destination account not found" });

        if (from.Balance < req.Amount)
            return BadRequest(new { message = "Insufficient funds" });

        from.Balance -= req.Amount;
        to.Balance += req.Amount;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Transferred {Amount} from {From} to {To}",
            req.Amount, req.FromAccountNumber, req.ToAccountNumber);

        return Ok(new
        {
            from = from.AccountNumber,
            to = to.AccountNumber,
            fromBalance = from.Balance,
            toBalance = to.Balance
        });
    }

    /// <summary>PUT /api/accounts/{id} — update an existing account. Requires a valid token.</summary>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Account updated)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return NotFound();

        account.OwnerName = updated.OwnerName;
        account.AccountType = updated.AccountType;
        account.Balance = updated.Balance;
        account.Currency = updated.Currency;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Updated account {AccountId}", id);
        return NoContent();
    }

    /// <summary>DELETE /api/accounts/{id} — close an account. Admin role only.</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _db.Accounts.FindAsync(id);
        if (account is null) return NotFound();

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Closed account {AccountId}", id);
        return NoContent();
    }
}
