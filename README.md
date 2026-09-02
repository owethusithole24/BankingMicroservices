# Banking Microservices (ITRI623)

A microservices-based digital banking application. **Step 1** ships the first
domain service — the **Account Service** — with its own PostgreSQL database,
all running in Docker containers.

See `ROADMAP.md` for the full build plan (gateway, auth, transactions, Consul, Seq).

## Prerequisites

- Docker Desktop (running)
- .NET 8 SDK — only needed if you want to run the service *outside* Docker

## Run it (the easy way — everything in Docker)

From this folder:

```bash
docker compose up --build
```

Then open:

- **Swagger UI:** http://localhost:5001/swagger
- **Accounts API:** http://localhost:5001/api/accounts
- **Health check:** http://localhost:5001/health

Stop with `Ctrl+C`, then:

```bash
docker compose down          # stop containers
docker compose down -v       # also wipe the database volume (fresh start)
```

## Run the service locally, DB in Docker (faster dev loop)

Start only the database:

```bash
docker compose up account-db
```

In another terminal, run the API from source:

```bash
cd AccountService
dotnet run
```

(The default connection string in `appsettings.json` points at `localhost:5432`,
which matches the exposed DB port.)

## What's in here

```
BankingMicroservices/
├── docker-compose.yml          # Account Service + its PostgreSQL DB
├── ROADMAP.md                  # full build plan + grading map
├── README.md
└── AccountService/
    ├── AccountService.csproj    # .NET 8 web API + EF Core (Npgsql) + Serilog + Swagger + health checks
    ├── Program.cs               # startup: DB, logging, Swagger, /health
    ├── Dockerfile
    ├── Models/Account.cs        # the Account entity
    ├── Data/AccountDbContext.cs # EF Core context (owns the accounts table)
    ├── Controllers/AccountsController.cs  # REST CRUD endpoints
    └── AccountService.http      # sample requests you can fire
```

## Patterns already demonstrated

- **Database per service** — the Account Service owns `accountdb`; nothing else touches it.
- **Centralised/structured logging** — Serilog logs every request and business event.
- **Health checks** — `/health` reports service + database reachability.

## Next step

Open `ROADMAP.md` → **Step 3** to put the Ocelot API Gateway in front of this service.
