# ITRI623 — Banking Microservices Build Roadmap

**Domain:** Digital Banking (finance)
**Stack:** .NET 8 (C#) · Ocelot API Gateway · Consul registry · PostgreSQL · Docker Compose · Serilog + Seq
**Machine:** M1 MacBook Air (Apple Silicon)
**Due:** Week of 31 August 2026

> **The golden rule:** build ONE vertical slice, get it running, then add ONE more piece at a time.
> The app should *run* at the end of every step. Never wire everything at once.

---

## The target system

| Service | Responsibility | Own database | Key endpoints |
|---|---|---|---|
| **Auth/Customer Service** | Register customers, login, issue JWT | Customer DB | `POST /api/auth/login`, `POST /api/customers`, `GET /api/customers/{id}` |
| **Account Service** | Bank accounts + balances | Account DB | `GET/POST /api/accounts`, `GET/PUT/DELETE /api/accounts/{id}` |
| **Transaction Service** | Deposits, withdrawals, transfers | Transaction DB | `POST /api/transactions`, `GET /api/transactions/{id}` |
| **API Gateway (Ocelot)** | Single entry point, routing, JWT validation | — | routes `/api/**` to the right service |
| **Consul** | Service registry / discovery | — | infra container |
| **Seq** | Centralised log viewer | — | infra container (http://localhost:5341) |

**Demo workflow (Section 14):** customer logs in → gets JWT → creates an account → makes a transfer → Transaction Service calls Account Service to adjust balances → logs in Seq show the whole flow.

**Patterns covered (need 2, you'll have 3+):** Database-per-service · Centralised logging · JWT security · (bonus) Health checks · (bonus) Retry/circuit-breaker.

---

## Step 0 — Environment  ✅ mostly done

- [x] Docker Desktop installed
- [ ] `brew install --cask dotnet-sdk` → verify `dotnet --version` shows 8.x
- [ ] IDE: VS Code + **C# Dev Kit** extension (or JetBrains Rider, free with NWU student email)
- [ ] Open Docker Desktop once so its engine is running
- [ ] Create the solution folder `BankingMicroservices/`

---

## Step 1 — Account Service running (no gateway yet)

*This is the scaffold I gave you. Everything else is a repeat of this pattern.*

- [ ] Unzip the scaffold into `BankingMicroservices/`
- [ ] From `BankingMicroservices/`, run: `docker compose up --build`
- [ ] Open **http://localhost:5001/swagger** — you should see the Accounts API
- [ ] Test `GET /api/accounts` → returns the two seeded accounts
- [ ] Test `POST /api/accounts` → create one, then `GET` it back
- [ ] Open **http://localhost:5001/health** → returns `Healthy`
- [ ] Stop with `Ctrl+C`, then `docker compose down`

**What to understand here:** each service owns its own Postgres database (database-per-service). The service never touches another service's DB — that's what gives you loose coupling and independent deployment. Be ready to explain this in the report.

---

## Step 2 — (already folded into Step 1)

The scaffold ships with a `Dockerfile` + `docker-compose.yml`, so Step 1 already containerises the service and its database. ✅ Containerisation loop learned.

---

## Step 3 — Put the API Gateway (Ocelot) in front

- [ ] Add a new project `ApiGateway/` (`dotnet new web`)
- [ ] Add package `Ocelot`
- [ ] Add `ocelot.json` with one route: `/api/accounts` → `account-service:8080`
- [ ] Add the gateway as a container in `docker-compose.yml` (port `5000:8080`)
- [ ] Verify: **http://localhost:5000/api/accounts** now reaches the Account Service *through the gateway*

**Understand:** the client only ever talks to the gateway (single entry point). The gateway decides which backend service handles each route.

---

## Step 4 — Auth Service + JWT security  *(pattern #2/#3)*

- [ ] Create `AuthService/` (same shape as Account Service, own Customer DB)
- [ ] `POST /api/auth/login` validates credentials and returns a signed **JWT**
- [ ] Add JWT bearer authentication to the Account Service; mark write endpoints `[Authorize]`
- [ ] Gateway forwards the `Authorization` header (and optionally validates the token)
- [ ] Verify: calling a protected endpoint without a token → `401`; with a valid token → works
- [ ] Log **failed logins** and **401s** — this feeds your SOC prep (Section 9)

---

## Step 5 — Transaction Service + inter-service call

- [ ] Create `TransactionService/` (own Transaction DB)
- [ ] `POST /api/transactions` records a transfer, then calls the **Account Service** over HTTP to adjust balances
- [ ] Use a typed `HttpClient` (via `IHttpClientFactory`) for the call
- [ ] Add the route to the gateway
- [ ] Verify the full workflow: login → create account → transfer → balances update

**Understand:** "services communicate where necessary" — this is the one place two services talk, and they do it over the API, never by sharing a database.

---

## Step 6 — Service registry (Consul) + centralised logging (Seq)  *(pattern locked in)*

- [ ] Add a **Consul** container to compose
- [ ] Each service registers itself with Consul on startup (package `Consul` / `Winton.Extensions.Configuration.Consul` or a small registration call)
- [ ] Point Ocelot at Consul for service discovery (`ServiceDiscoveryProvider` in `ocelot.json`)
- [ ] Add a **Seq** container; switch Serilog to also write to Seq (`Serilog.Sinks.Seq`)
- [ ] Verify: all services appear in Consul UI (http://localhost:8500) and all logs land in Seq (http://localhost:5341)
- [ ] Confirm logs include: request received/completed, errors, failed auth, inter-service calls (Section 6.2)

---

## Step 7 — (Optional but cheap marks) Resilience + metrics

- [ ] Add **Polly** retry/circuit-breaker around the Transaction→Account HTTP call
- [ ] Expose Prometheus metrics (`prometheus-net.AspNetCore`) or use `/metrics`
- [ ] Each of these is another recognised pattern for Section 6

---

## Step 8 — Deliverables (Section 13)

- [ ] **Source code** — all services + gateway + compose files
- [ ] **Deployment files** — `docker-compose.yml`, Dockerfiles, env config
- [ ] **API documentation** — Swagger is already generating it; export/screenshot it
- [ ] **Architecture diagram** — client → gateway → services → databases + Consul + Seq (I can generate this for you)
- [ ] **Demo video / live demo** — show startup, registration in Consul, a request through the gateway, one full workflow, logs in Seq
- [ ] **Technical report** — answer the 9 questions in Section 13.6 (I can help draft this)

---

## Grading map (Section 15) — where your marks come from

| Criterion | Weight | Covered by |
|---|---|---|
| Functional application | 20% | Steps 1, 4, 5 (working workflow) |
| Endpoint design | 15% | Step 1 pattern (clean REST + Swagger) |
| API Gateway | 15% | Step 3 |
| Deployment | 15% | Steps 1–2, all compose work |
| Service registry/discovery | 10% | Step 6 (Consul) |
| Microservices patterns | 10% | DB-per-service + logging + JWT |
| Prep for SOC/KG | 5% | Logging failed logins, transfers, errors |

**Note:** nothing rewards a *complicated* domain. A simple banking system done cleanly scores full marks. Keep each service small.
