# Item Authoring Platform

A production-grade web application for authoring assessment items and assembling them into
examinations, built so that **one domain and one application layer are exposed through two
independent API surfaces — REST and GraphQL — and consumed by one Angular client that can switch
between them at runtime.**

That last property is the point. The client's entire feature set runs over either transport without a
single duplicated component, route or form, which is what makes a like-for-like comparison of the two
API styles methodologically valid rather than anecdotal.

---

## Table of contents

- [What it does](#what-it-does)
- [Prerequisites](#prerequisites)
- [Getting started](#getting-started)
- [Architecture](#architecture)
- [Design decisions and their trade-offs](#design-decisions-and-their-trade-offs)
- [The two API surfaces](#the-two-api-surfaces)
- [Switching transports in the client](#switching-transports-in-the-client)
- [Measuring the two surfaces](#measuring-the-two-surfaces)
- [Security](#security)
- [Testing](#testing)
- [Command reference](#command-reference)
- [Repository layout](#repository-layout)

---

## What it does

**Item management.** Authors create, edit, preview, categorise, tag and delete assessment items in
four answer shapes — multiple choice with a single correct option, multiple choice with several
correct options, either/or, and essay. Items move through an editorial lifecycle
(`Draft → In review → Approved → Published → Retired`) and every publication freezes an immutable,
numbered version, so an exam assembled last term still contains what candidates actually saw.

**Exam building.** Instructors assemble examinations from published items: sections, drag-and-drop
ordering, per-exam score overrides, composition validation and a preview of the assembled paper.

**Administration.** Administrators manage user accounts, roles and the permissions each role grants.

**Security.** JWT bearer authentication with rotating refresh tokens, and permission-based
authorization enforced in the application layer — never in a controller or a resolver — so both API
surfaces enforce byte-for-byte the same rules.

---

## Prerequisites

| Tool | Version | Why |
| --- | --- | --- |
| .NET SDK | 10.0 or later | Builds and runs the backend |
| Node.js | 20.19+, 22.12+ or 24.x | Builds and runs the Angular client |
| SQL Server | 2022 or 2025 (LocalDB is fine for development) | The database |
| Docker | Any recent version | Optional: the integration tests and the full stack |

Everything below assumes PowerShell on Windows; the commands are the same on macOS and Linux with
`./scripts/*.ps1` run through `pwsh`.

---

## Getting started

### 1. Generate the development secrets

The repository contains **no** signing key and **no** administrator password. Generate your own:

```powershell
./scripts/init-dev-secrets.ps1
```

The script writes them to the per-developer user-secret store (outside the repository) and prints the
administrator password once. Record it.

### 2. Run the backend

```powershell
dotnet run --project src/ItemAuthoring.Api
```

On first start the API applies its migrations, seeds the permission catalogue, the four platform roles
and the administrator account, and — in `Development` — generates a sample item bank so there is
something to look at.

| Endpoint | Address |
| --- | --- |
| REST | `https://localhost:7175/api/v1` |
| Swagger UI | `https://localhost:7175/swagger` |
| GraphQL | `https://localhost:7175/graphql` |
| GraphQL IDE | `https://localhost:7175/graphql` in a browser (development only) |
| Health | `https://localhost:7175/health` |

### 3. Run the client

```powershell
cd client
npm install
npm start
```

The client is served at `http://localhost:4200` and proxies `/api` and `/graphql` to the backend, so
the browser sees a single origin.

### 4. Or run the whole stack in containers

```powershell
Copy-Item .env.example .env    # then fill in the three blank values
docker compose up --build
```

The client is then at `http://localhost:8080` and the API at `http://localhost:5080`.

---

## Architecture

Clean Architecture with CQRS. Dependencies point inwards only, and the compiler enforces it: the
domain project has no package references at all, and the application project has no reference to
Entity Framework Core or to ASP.NET Core.

```
src/
  ItemAuthoring.Domain/          entities, value objects, invariants, domain events — zero dependencies
  ItemAuthoring.Application/     commands, queries, handlers, validators, abstractions, DTOs
  ItemAuthoring.Infrastructure/  EF Core, repositories, read stores, identity, JWT, migrations, seeding
  ItemAuthoring.Api/             composition root: REST controllers + GraphQL schema + middleware
tests/
  ItemAuthoring.Domain.Tests/        invariants and lifecycle rules
  ItemAuthoring.Application.Tests/   handlers, pipeline behaviours, query composition
  ItemAuthoring.Integration.Tests/   the real pipeline against real SQL Server (Testcontainers)
client/
  src/app/core/          guards, interceptors, auth, transport selection, error normalization, metrics
  src/app/features/      items, exams, administration, auth, benchmark — lazy loaded standalone routes
  src/app/shared/        dumb components and the view models the gateway contract owns
  src/app/data-access/
    gateways/            the transport-agnostic contract
    rest/                HttpClient implementations
    graphql/             Apollo implementations
```

### The request pipeline

Every use case is a `record` implementing `ICommand<T>` or `IQuery<T>`, dispatched through an
in-house mediator. Four behaviours wrap it, outside-in:

1. **Authorization** — reads the permission declared on the request with `[RequiresPermission]`.
   Requests are authenticated by default; opting out requires an explicit `[AllowAnonymousRequest]`.
2. **Validation** — runs the FluentValidation rules and returns per-field details.
3. **Logging** — records the request, its outcome and its duration.
4. **Domain exception translation** — turns a violated invariant into a failed `Result` carrying the
   domain's own stable error code.

Because all four sit *inside* the application layer, a controller and a GraphQL resolver cannot
enforce different rules. Neither surface contains a single `if` that the other does not.

---

## Design decisions and their trade-offs

These are the decisions a reviewer is most likely to question, with the reasoning that led to them.

### Table-per-hierarchy for the item types

**Alternatives.** Table-per-type gives each answer shape its own table and no nullable columns;
table-per-hierarchy puts all four in one table with a discriminator.

**Decision: table-per-hierarchy.** Every read in this application is polymorphic — the item bank grid,
the exam builder's picker and the GraphQL `items` field all query across all four shapes. Under
table-per-type each of those becomes a four-way join or a union, which would add a persistence
artefact to precisely the queries the study measures. The price is a handful of nullable
essay-specific columns, which a check constraint and filtered indexes keep honest.

### Unit of Work only where it earns its place

`DbContext` already *is* a unit of work. `IUnitOfWork` exists for exactly one reason: to keep the
application layer free of an Entity Framework Core reference. It adds no behaviour. An explicit
transaction is opened in exactly one use case — refresh-token rotation, which must revoke and issue
inside one atomic step — and nowhere else, because saving once is already atomic.

### No AutoMapper

Mapping in this application is either a projection expressed directly in an Entity Framework
expression tree (the read stores) or a three-line factory (the authentication response). Both are
compile-time checked and both are readable in place. A mapping framework would move that logic into
convention-driven profiles, cost a startup-time configuration scan, and turn a compiler error into a
runtime one. It was not adopted.

### An in-house mediator instead of a library

The only feature needed is "resolve one handler and wrap it in an ordered list of behaviours" — about
sixty lines. Taking a dependency for that would add a licence constraint and an upgrade obligation
without removing any real complexity. Reflection is confined to a one-off closed-generic wrapper per
request type, which is cached, so steady-state dispatch is a dictionary lookup and a virtual call.
That matters, because dispatch sits inside every measurement the study reports.

### Strongly typed identifiers

`ItemId` and `ExamId` are distinct types, so passing one where the other is expected does not compile.
A single generic value converter serves all of them, so the cost of avoiding primitive obsession stays
constant instead of growing with the model. Data transfer objects expose plain `Guid`s, so the domain
types never reach an API surface.

### Read models, not aggregates, on the read side

Queries never load an aggregate. The read stores return a composable `IQueryable<T>` over an already
projected DTO, so filtering, sorting and paging reach SQL Server — for the REST query handler and for
the Hot Chocolate middleware alike.

### Expected failures are returned, not thrown

"Not found", "already published" and "not permitted" are outcomes, not exceptions. They are modelled
as a `Result` with a stable code, which is what allows REST and GraphQL to publish the *same* code for
the same failure. Exceptions remain reserved for genuinely exceptional conditions.

---

## The two API surfaces

Both are first-class and reach feature parity. Neither contains a business rule.

### REST

- Resource-oriented, versioned routes (`/api/v1/...`) with correct status codes
- RFC 9457 `ProblemDetails` for every failure, carrying a stable `code` and the correlation identifier
- Offset paging with metadata, plus filtering, searching and sorting through query parameters
- `ETag` and `If-None-Match` on single-resource reads, with `Cache-Control`
- Complete OpenAPI documentation with bearer authentication wired into Swagger UI
- Rate limiting, applied far more aggressively to the authentication endpoints

### GraphQL

- Code-first schema with queries, mutations and a subscription for item publication
- Relay-style cursor paging, filtering and sorting middleware that push predicates into SQL
- Offset-paged `searchItems`, `searchExams` and `searchUsers` fields that dispatch the *same* query
  objects the REST controllers dispatch, so the two surfaces can be compared like for like
- A data loader behind every collection navigation, so a list query costs a bounded number of
  statements rather than one per row
- A maximum execution depth, so a recursive query cannot become a denial of service vector
- The GraphQL IDE is enabled outside production only

Hot Chocolate 16 no longer ships offset paging middleware; the offset-paged fields above provide it
explicitly, which is also what keeps them identical to the REST endpoints.

---

## Switching transports in the client

Every feature component injects an **abstract gateway** — `ItemsGateway`, `ExamsGateway`,
`UsersGateway`, `AuthGateway`, `TaxonomyGateway` — whose methods are expressed purely in domain terms.
No HTTP, no GraphQL document and no transport type appears in any signature.

Each contract is implemented twice. A **router** resolves the active implementation *per call* from
`TransportService`, so flipping the toolbar selector changes the behaviour of components that are
already on screen. The router forwards through a proxy rather than a hand written delegating method
per contract member: a hand written router that forgot a method would silently keep sending it over
one transport, and the resulting measurements would be wrong in a way no test would notice.

Screens include the active transport in the parameters of their `rxResource`, so a switch immediately
re-runs the current view over the other surface. The Apollo cache is cleared on every switch, so a
GraphQL run is never served data a REST run put in memory.

The selector is rendered **once**, in the shell toolbar. It is visible in development and in the
`Benchmark` environment, and hidden in production — where the transport is pinned to
`environment.defaultTransport` so the experiment tooling cannot leak into a real deployment.

---

## Measuring the two surfaces

Both ends are instrumented.

**Server side.** `RequestMetricsMiddleware` sits outside routing and records, per request: the
transport, the logical operation, the status code, the wall-clock duration, the uncompressed response
size and — the figure a client cannot observe — the number of database round trips the request caused.

| Endpoint | Purpose |
| --- | --- |
| `GET /api/v1/benchmark/measurements` | every retained measurement, as JSON |
| `GET /api/v1/benchmark/measurements.csv` | the same, as CSV |
| `GET /api/v1/benchmark/summary` | median, 95th percentile, mean payload and mean statement count per transport and operation |
| `DELETE /api/v1/benchmark/measurements` | start a run from a clean slate |

A GraphQL request always arrives as `POST /graphql`, so the client names the logical operation in the
`X-Benchmark-Operation` header; without it the server could not tell "load the item list" from "load
one item".

**Client side.** `MetricsCollector` records duration, request count and payload size at the *gateway*
boundary — deliberately, because that is where transport specific work such as response reshaping
begins and ends. Mapping cost therefore lands on the transport that incurs it. The **Benchmark**
screen drives the same gateway calls once per transport and exports the results as CSV.

To run a measurement session:

```powershell
Copy-Item .env.example .env      # fill in the blanks; keep API_ENVIRONMENT=Benchmark
docker compose up --build
# open http://localhost:8080/benchmark, set the iteration count, press Run, export the CSV
```

---

## Security

The OWASP Top Ten, addressed explicitly:

- **Broken access control** — authorization is enforced in the application layer, not at the
  transport, so it cannot be bypassed by choosing the other API. Ownership rules ("may this person
  edit *this* item") live next to the loaded aggregate.
- **Cryptographic failures** — passwords are hashed with the platform PBKDF2 implementation; refresh
  tokens are stored only as SHA-256 hashes; HTTPS and HSTS are enforced outside development.
- **Injection** — every query goes through Entity Framework Core parameterisation. There is no
  concatenated SQL anywhere in the repository.
- **Insecure design** — item and exam lifecycles are explicit state machines; illegal transitions are
  impossible rather than merely discouraged.
- **Security misconfiguration** — options are validated at startup, so a missing or short signing key
  fails the process instead of silently producing forgeable tokens. Containers run as a non-root user.
- **Identification and authentication failures** — sign-in returns one indistinguishable error for an
  unknown account and a wrong password, advances a lockout counter, and rotates refresh tokens.
  Presenting an already-rotated token revokes the whole family. Rate limiting protects the endpoints.
- **Software and data integrity** — published item versions are immutable; deleting an item that an
  exam references is refused by a foreign key rather than silently rewriting the exam.
- **Logging and monitoring failures** — every request carries a correlation identifier that is echoed
  to the caller, written to the log and quoted in error responses.

**No credential is committed.** The signing key, the administrator password and the database password
are supplied through user secrets or the environment; `.env` is git-ignored and `.env.example`
documents what must be filled in.

---

## Testing

| Suite | What it covers | How to run |
| --- | --- | --- |
| Domain | invariants of all four answer shapes, both lifecycles, token rotation, lockout | `dotnet test tests/ItemAuthoring.Domain.Tests` |
| Application | pipeline behaviours, handlers, query composition, error classification | `dotnet test tests/ItemAuthoring.Application.Tests` |
| Integration | the real pipeline against real SQL Server, including REST/GraphQL parity | `dotnet test tests/ItemAuthoring.Integration.Tests` |
| Client unit | transport service, selector, error normalization, form rules | `cd client; npm run test:ci` |
| Gateway contract | one shared suite executed against **both** implementations | part of `npm run test:ci` |
| End-to-end | full journeys, run once per transport, with accessibility assertions | `cd client; npm run e2e` |

The integration suite needs a container runtime. Without one its tests report as **skipped** rather
than failed, so `dotnet test` remains meaningful on a machine without Docker; continuous integration
always has one.

The parity tests are the ones that justify the architecture: each performs the same logical operation
twice, once per transport, and asserts the observable result is identical — same payload, same error
code, same authorization outcome.

### Coverage

```powershell
dotnet test ItemAuthoring.slnx --settings coverlet.runsettings --results-directory artifacts/coverage
./scripts/report-coverage.ps1 -MinimumPercentage 80
```

`coverlet.runsettings` excludes the Entity Framework Core migrations and the model snapshot, because
measuring generated code reports a number about the scaffolder rather than about the code under
review.

The 80% gate is enforced in continuous integration, where a container runtime is available and the
integration suite therefore runs. On a developer machine without Docker those tests are skipped, the
API and infrastructure layers they exercise appear untested, and the local figure will be lower — by
design rather than by accident.

---

## Command reference

### Backend

| Command | Purpose |
| --- | --- |
| `dotnet restore ItemAuthoring.slnx` | Restore packages |
| `dotnet build ItemAuthoring.slnx` | Build (warnings are errors) |
| `dotnet test ItemAuthoring.slnx` | Run every test suite |
| `dotnet run --project src/ItemAuthoring.Api` | Run the API |
| `dotnet ef migrations add <Name> --project src/ItemAuthoring.Infrastructure --startup-project src/ItemAuthoring.Api --output-dir Persistence/Migrations` | Add a migration |
| `dotnet ef database update --project src/ItemAuthoring.Infrastructure --startup-project src/ItemAuthoring.Api` | Apply migrations |
| `./scripts/export-contracts.ps1` | Refresh `artifacts/openapi.json` and `artifacts/schema.graphql` |
| `./scripts/init-dev-secrets.ps1` | Generate and store the development secrets |

### Client

| Command | Purpose |
| --- | --- |
| `npm install` | Restore dependencies |
| `npm start` | Dev server with the proxy pointed at the API |
| `npm run build` | Production build (AOT, budgets enforced, output hashing) |
| `npm run watch` | Development build in watch mode |
| `npm test` | Unit and component tests, in watch mode |
| `npm run test:ci` | Headless single run with coverage |
| `npm run lint` | ESLint with `angular-eslint` |
| `npm run e2e` | Playwright suite, over both transports |
| `npm run codegen` | Regenerate the typed REST client and the typed GraphQL operations |

`npm run codegen` reads the two committed artefacts rather than a running server, so it — and
therefore the client build — is reproducible offline. Continuous integration regenerates both and
fails if the committed copies are stale, which is what stops the client and the schema drifting apart.

---

## Repository layout

```
.github/workflows/ci.yml     build, test, lint, contract-freshness and end-to-end jobs
artifacts/                   the OpenAPI document, the GraphQL schema and the SQL schema script
client/                      the Angular application
scripts/                     developer scripts (secrets, contract export)
src/                         the four backend projects
tests/                       the three backend test projects
docker-compose.yml           SQL Server + API + client
Directory.Build.props        nullable, latest analysers, warnings as errors, XML documentation
Directory.Packages.props     central package version management
```
