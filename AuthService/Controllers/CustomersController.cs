using AuthService.Data;
using AuthService.Models;
using AuthService.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

/// <summary>Customer registration and lookup.</summary>
[ApiController]
[Route("api/[controller]")] // -> /api/customers
public class CustomersController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(AuthDbContext db, ILogger<CustomersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>GET /api/customers — list customers (without password hashes).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _db.Customers
            .Select(c => new { c.Id, c.Username, c.FullName, c.Role, c.CreatedAt })
            .ToListAsync();
        return Ok(customers);
    }

    /// <summary>GET /api/customers/{id} — fetch one customer.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c is null) return NotFound();
        return Ok(new { c.Id, c.Username, c.FullName, c.Role, c.CreatedAt });
    }

    /// <summary>POST /api/customers — register a new customer.</summary>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await _db.Customers.AnyAsync(c => c.Username == req.Username))
            return Conflict(new { message = "Username already taken" });

        var customer = new Customer
        {
            Username = req.Username,
            FullName = req.FullName,
            PasswordHash = PasswordHasher.Hash(req.Password),
            Role = string.IsNullOrWhiteSpace(req.Role) ? "Customer" : req.Role
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Registered new customer {Username}", customer.Username);

        return CreatedAtAction(nameof(GetById), new { id = customer.Id },
            new { customer.Id, customer.Username, customer.FullName, customer.Role });
    }
}
