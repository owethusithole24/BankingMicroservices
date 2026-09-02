using AuthService.Data;
using AuthService.Infrastructure;
using AuthService.Models;
using AuthService.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("AuthDb");
builder.Services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "auth-db");

// Register this service with Consul so the gateway can discover it.
builder.Services.AddConsulRegistration(builder.Configuration);

var app = builder.Build();

// Create tables and seed two demo users on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.EnsureCreated();

    if (!db.Customers.Any())
    {
        db.Customers.AddRange(
            new Customer { Username = "admin", FullName = "Bank Administrator", Role = "Admin",    PasswordHash = PasswordHasher.Hash("admin123") },
            new Customer { Username = "thabo", FullName = "Thabo Mokoena",      Role = "Customer", PasswordHash = PasswordHasher.Hash("password123") }
        );
        db.SaveChanges();
    }
}

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
