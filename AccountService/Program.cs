using System.Text;
using AccountService.Data;
using AccountService.Infrastructure;
using AccountService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Centralised, structured logging (Serilog) ---
// Writes to the console for now. In Step 6 you add a Seq sink so all services
// log to one place. This already satisfies "consistent logging approach" (6.2).
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

// --- EF Core + PostgreSQL (this service's OWN database) ---
var connectionString = builder.Configuration.GetConnectionString("AccountDb");
builder.Services.AddDbContext<AccountDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- JWT authentication: validate tokens issued by the Auth Service ---
// We share the same signing Key/Issuer/Audience with the Auth Service, so this
// service can verify a token on its own without ever calling the Auth Service.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

// Swagger, with a "Bearer" box so you can paste a token and test protected endpoints.
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT here (without the word 'Bearer')."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// --- Health checks: reports whether the service AND its database are reachable (6.5) ---
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "account-db");

// --- Register this service with Consul so the gateway can discover it ---
builder.Services.AddConsulRegistration(builder.Configuration);

var app = builder.Build();

// --- Create the database/tables on startup and seed sample data ---
// EnsureCreated() is the simplest option for a student project. Later you can
// switch to EF migrations (dotnet ef migrations add ...) for production-style versioning.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    db.Database.EnsureCreated();

    if (!db.Accounts.Any())
    {
        db.Accounts.AddRange(
            new Account { AccountNumber = "1000000001", OwnerName = "Thabo Mokoena",  AccountType = "Cheque",  Balance = 15200.50m },
            new Account { AccountNumber = "1000000002", OwnerName = "Lerato Dlamini", AccountType = "Savings", Balance = 48250.00m }
        );
        db.SaveChanges();
    }
}

// Log every incoming HTTP request/response in one tidy line.
app.UseSerilogRequestLogging();

// Swagger is enabled in all environments so graders can browse the API.
app.UseSwagger();
app.UseSwaggerUI();

// Authentication must come before authorization, and both before the controllers.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
