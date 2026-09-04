using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging so we can see the gateway routing each request.
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

// Load the routing table (ocelot.json) and register Ocelot with Consul-based service discovery.
// .AddConsul() lets Ocelot look up service addresses from Consul at request time,
// instead of the hard-coded host:port it used before.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration).AddConsul();

// Allow the browser-based frontend to call the gateway (CORS).
// The frontend runs on a different origin (http://localhost:8090), so the gateway
// must send CORS headers. Auth is via bearer token, so allowing any origin is fine here.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// CORS must run before Ocelot takes over the pipeline.
app.UseCors();

app.UseSerilogRequestLogging();

// A tiny root endpoint so hitting the gateway's base URL shows it's alive.
app.MapGet("/", () => "Banking API Gateway is running. Try /api/accounts");

// Ocelot must be the LAST thing in the pipeline — it takes over routing from here.
await app.UseOcelot();

app.Run();
