using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Models;
using TransactionService.Services;

namespace TransactionService.Controllers;

/// <summary>Money transfers. Creating a transfer requires a valid JWT.</summary>
[ApiController]
[Route("api/[controller]")] // -> /api/transactions
public class TransactionsController : ControllerBase
{
    private readonly TransactionDbContext _db;
    private readonly AccountServiceClient _accounts;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        TransactionDbContext db,
        AccountServiceClient accounts,
        ILogger<TransactionsController> logger)
    {
        _db = db;
        _accounts = accounts;
        _logger = logger;
    }

    /// <summary>GET /api/transactions — list all transaction records.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Transactions.OrderByDescending(t => t.CreatedAt).ToListAsync());

    /// <summary>GET /api/transactions/{id} — one transaction record.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tx = await _db.Transactions.FindAsync(id);
        return tx is null ? NotFound() : Ok(tx);
    }

    /// <summary>POST /api/transactions — perform a transfer. Requires a valid token.</summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionRequest req)
    {
        if (req.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than zero" });

        // 1. Record the attempt in our OWN database first (status Pending).
        var tx = new Transaction
        {
            FromAccountNumber = req.FromAccountNumber,
            ToAccountNumber = req.ToAccountNumber,
            Amount = req.Amount,
            Reference = req.Reference,
            Status = "Pending"
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();

        // A large transfer is a security-relevant event for the future SOC layer (§9).
        if (req.Amount >= 10000m)
            _logger.LogWarning("Large transfer flagged: {Amount} from {From} to {To} (tx {TxId})",
                req.Amount, req.FromAccountNumber, req.ToAccountNumber, tx.Id);

        // 2. Ask the Account Service to actually move the money (inter-service call),
        //    forwarding the caller's JWT.
        var authHeader = Request.Headers.Authorization.ToString();
        var result = await _accounts.TransferAsync(
            new TransferRequest(req.FromAccountNumber, req.ToAccountNumber, req.Amount),
            authHeader);

        // 3. Update our record with the outcome.
        if (result.Success)
        {
            tx.Status = "Completed";
            _logger.LogInformation("Transaction {TxId} completed: {Amount} {From} -> {To}",
                tx.Id, tx.Amount, tx.FromAccountNumber, tx.ToAccountNumber);
        }
        else
        {
            tx.Status = "Failed";
            tx.FailureReason = result.Error;
            _logger.LogWarning("Transaction {TxId} failed: {Reason}", tx.Id, result.Error);
        }
        await _db.SaveChangesAsync();

        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx)
            : StatusCode(StatusCodes.Status422UnprocessableEntity, tx);
    }
}
