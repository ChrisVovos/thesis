---
description: 'Build the complete Item Authoring web application (ASP.NET Core + Angular + SQL Server) exposing both REST and GraphQL APIs, per the requirements in command.txt.'
mode: agent
---

# Item Authoring Platform — Implementation Prompt

## Role

You are a senior software architect and implementation engineer. You are **building the software**, not writing the thesis. The academic write-up is a later, separate deliverable. Optimize every response for producing real, compiling, runnable code.

`command.txt` in this workspace is the **single source of truth** for requirements. Read it before making any decision. Where this prompt and `command.txt` disagree, `command.txt` wins — except for the explicitly resolved conflicts listed under *Resolved Ambiguities* below.

## Prime Directive

Produce a **complete, working, production-grade application**. Never emit:

- pseudocode
- `// TODO`, `// implement later`, `throw new NotImplementedException()`
- placeholder strings, stub methods, or empty handlers
- illustrative snippets in chat instead of real files on disk

Every file you create must compile, every endpoint must execute, every migration must apply, every screen must render against the live API.

Explanations are permitted only as short justifications attached to a real change. Do not replace implementation with discussion.

## What Is Being Built

A professional Item Authoring system for exam content: authors create and maintain assessment items; reviewers approve them; instructors assemble items into examinations; administrators manage users and roles.

The same domain and application layer is exposed through **two independent API surfaces over one shared core** — REST and GraphQL — so the two approaches can later be measured against identical business logic. Neither surface may contain business rules; both are thin adapters.

## Technology Stack (non-negotiable)

| Concern | Technology |
| --- | --- |
| Backend runtime | ASP.NET Core (.NET 10), C# 13 |
| Persistence | Entity Framework Core 10, SQL Server 2025 |
| REST API | ASP.NET Core controllers or minimal APIs, documented with Swagger/OpenAPI |
| GraphQL API | Hot Chocolate (schema-first-quality code-first schema, DataLoaders, filtering/sorting/paging middleware) |
| Frontend | Angular 20+ (standalone components, signals), TypeScript 5 strict, Angular CLI `application` builder |
| GraphQL client | Apollo Angular + GraphQL Code Generator |
| REST client | Angular `HttpClient` + OpenAPI-generated typed client |
| Auth | JWT bearer authentication, refresh tokens, role-based authorization |
| Validation | FluentValidation |
| Logging | Serilog with structured logging and correlation IDs |
| Testing | xUnit, NSubstitute, Testcontainers (SQL Server), Jest + Angular Testing Library, Playwright |

## Resolved Ambiguities

**Angular is the frontend framework**, per the `Use Angular.` instruction in the FRONTEND section of `command.txt`. It is the only frontend framework permitted in this project.

| Requirement in command.txt | Angular implementation |
| --- | --- |
| Angular Router | Angular Router with lazy-loaded standalone routes and functional guards |
| Forms + Validation | Typed Reactive Forms with synchronous and async validators mirroring the FluentValidation rules |
| State management | Angular signals for local/component state; a signal-based store service per feature; RxJS only at async boundaries |
| API Layer | A transport-agnostic gateway layer with REST and GraphQL implementations (see below) |
| Protected Routes | `canActivate`/`canMatch` functional guards driven by JWT claims and role checks |
| Reusable Components | Standalone shared component library under `src/app/shared/` |
| Loading/Error States | `HttpInterceptor` + Apollo error link feeding a shared notification and busy-indicator service |

`command.txt` also refers to "GraphDB" in the user request; this means **GraphQL**, not a graph database. Persistence is relational (SQL Server).

Use the Unit of Work pattern **only** where a use case genuinely spans multiple aggregates in one transaction; `DbContext` already is a Unit of Work, and this must be stated as a deliberate decision rather than layered on reflexively. Use AutoMapper only if mapping volume justifies it; otherwise prefer explicit mapping or source-generated mappers, and record the reasoning.

## Frontend Build & Tooling

The Angular client is a standard npm project driven entirely through `package.json` scripts. Every frontend action goes through npm — never invoke bundlers or test runners directly, and never hand-roll a bundler configuration. The Angular CLI `application` builder already handles esbuild-based production builds and the HMR dev server.

Define and use exactly these scripts:

| Script | Purpose |
| --- | --- |
| `npm install` | Restore dependencies |
| `npm start` | Dev server with proxy config pointing at the ASP.NET Core API |
| `npm run build` | Production build (AOT, budgets enforced, output hashing) |
| `npm run watch` | Development build in watch mode |
| `npm test` | Unit and component tests |
| `npm run test:ci` | Headless single run with coverage output |
| `npm run lint` | ESLint with `angular-eslint` |
| `npm run e2e` | Playwright end-to-end suite |
| `npm run codegen` | Regenerate the typed REST client from OpenAPI and the typed GraphQL operations |

`npm run build` must complete with zero errors and zero warnings before any slice is considered done, and it must be part of the CI pipeline alongside `dotnet build` and `dotnet test`.

## Architecture

Clean Architecture with CQRS, strict inward-pointing dependencies:

```
src/
  ItemAuthoring.Domain/          # entities, value objects, domain events, invariants — zero dependencies
  ItemAuthoring.Application/     # commands, queries, handlers, validators, abstractions, DTOs
  ItemAuthoring.Infrastructure/  # EF Core, repositories, identity, JWT, external services, migrations
  ItemAuthoring.Api/             # composition root; REST controllers + GraphQL schema + middleware
tests/
  ItemAuthoring.Domain.Tests/
  ItemAuthoring.Application.Tests/
  ItemAuthoring.Integration.Tests/
client/
  src/app/
    core/            # guards, interceptors, auth, config, transport selection
    features/        # items, exams, admin, auth — lazy-loaded standalone routes
    shared/          # dumb components, pipes, directives, models
    data-access/
      gateways/      # transport-agnostic abstract gateways (the contract)
      rest/          # HttpClient implementations + generated OpenAPI client
      graphql/       # Apollo implementations + generated operations/types
```

Mandatory across all layers:

- **SOLID** — every class has one reason to change; depend on abstractions defined in `Application`.
- Command/query separation with a mediator abstraction (do not take a dependency on MediatR; use a lightweight in-house or Mediator-source-generator dispatcher).
- Domain events raised in the domain, dispatched after successful persistence.
- Global exception handling middleware translating exceptions to RFC 9457 `ProblemDetails` for REST and to typed GraphQL errors for GraphQL — with parity of error codes between the two.
- Cross-cutting pipeline behaviours: validation, logging, performance timing, transaction scope.
- Configuration via `appsettings.json` + environment overrides + user secrets in development. Never commit secrets.
- Object Calisthenics discipline: files under ~200 lines, nesting depth ≤ 3–4, no primitive obsession in the domain.

## Domain Model

Item types to support, each with its own answer/scoring shape:

1. Multiple Choice — Single Response
2. Multiple Choice — Multiple Response
3. Essay
4. Either/Or

Model these as a polymorphic item hierarchy (EF Core table-per-hierarchy or table-per-type — choose one and justify it in a code comment/ADR-style note in the README).

Core aggregates and concepts: `Item`, `ItemVersion`, `Category`, `Tag`, `DifficultyLevel`, `Exam`, `ExamSection`, `ExamItem` (with ordering), `User`, `Role`, `Permission`.

Item lifecycle: Draft → In Review → Approved → Published → Retired, with transitions enforced in the domain and gated by role.

## Functional Scope

**Item management** — create, edit, delete (soft delete), preview, categorize, tag, assign difficulty, search, filter, sort, paginate, and version items with immutable published versions.

**Exam builder** — create exams, add existing items, remove items, reorder items, preview the assembled exam, validate exam composition rules.

**Users & security** — login, logout, refresh token rotation, role assignment. Roles: `Administrator`, `Instructor`, `Author`, `Reviewer`. Authorization must be policy-based and enforced in the application layer, not only at the transport layer, so REST and GraphQL enforce identical rules.

**Administration** — manage users, manage roles and permissions.

## Dual API Requirement

Both surfaces are first-class and must reach feature parity:

**REST**
- Resource-oriented routes, correct status codes, `ProblemDetails` errors
- Pagination (`page`/`pageSize` with metadata), filtering, searching, sorting via query parameters
- ETag/`If-None-Match` conditional requests and cache headers where meaningful
- Versioned routes and complete Swagger/OpenAPI documentation with auth support

**GraphQL**
- Code-first schema with queries, mutations, and subscriptions where real-time helps
- DataLoaders on every collection navigation to eliminate N+1
- Relay-style cursor pagination alongside offset pagination
- Projection, filtering, and sorting middleware pushing predicates into SQL
- Query complexity/depth limits and persisted queries as a production safeguard
- Banana Cake Pop / IDE enabled in development only

Both surfaces must be instrumented (request duration, payload size, resolved SQL query count) so a later comparison can be made from real measurements rather than assertions.

### Consuming Both Surfaces From One Angular Application

The Angular client must be able to run its **entire feature set** against either transport, switched at runtime, without changing a single component. This is what makes the comparison methodologically valid: identical UI, identical user actions, only the transport differs.

Implement it as follows:

1. **Define the contract once.** For each feature area declare an abstract gateway in `data-access/gateways/`, e.g. `ItemsGateway`, `ExamsGateway`, `UsersGateway`, `AuthGateway`. Methods are expressed purely in domain terms (`search(query: ItemQuery): Observable<PagedResult<ItemSummary>>`) with no HTTP, no GraphQL document, and no transport type leaking into the signature. All view models live in `shared/models/` and are owned by the contract, not by either transport.
2. **Implement it twice.** `RestItemsGateway` uses the OpenAPI-generated client over `HttpClient`; `GraphQlItemsGateway` uses Apollo Angular with codegen-typed documents. Each is responsible for mapping its own wire format to the shared view models, so mapping cost is measured as part of the transport, where it genuinely belongs.
3. **Select at runtime.** A `TransportService` holds the active transport as a signal, persisted to `localStorage` and seeded from `environment.defaultTransport`. Register each gateway with a factory provider that resolves the concrete implementation from that service. Components inject only the abstract class — this is the Dependency Inversion Principle doing the real work, and the entire comparison rests on it.
4. **Expose the switch.** A transport selector in the app shell toolbar flips REST ↔ GraphQL live, so the same screen can be exercised both ways within one session. Its full specification is below.
5. **Share cross-cutting concerns.** One auth token store; a `JwtInterceptor` for REST and an Apollo `authLink` for GraphQL, both reading that store; one correlation-ID mechanism; one error normalizer that converts `ProblemDetails` and GraphQL error extensions into the same `AppError` shape before it reaches the UI.
6. **Measure at the boundary.** A shared `MetricsCollector` records, per logical operation: request count, wall-clock duration, and uncompressed and compressed payload size — recorded in the interceptor and Apollo link respectively, tagged with the active transport, and exportable as CSV/JSON for later analysis.
7. **Test both.** Every gateway contract gets one shared test suite executed twice, once per implementation, asserting identical outputs for identical inputs. Playwright end-to-end suites run the full journey under both transports.

Do not duplicate feature components, routes, or forms per transport. Duplication anywhere above the gateway layer invalidates the comparison and will be treated as a defect.

### Transport Selector (Toolbar Dropdown)

The switch that decides whether a call such as `getById` goes to REST or GraphQL is a **single dropdown rendered once in the application shell toolbar**. Implement it exactly as specified here.

**`TransportService`** — `src/app/core/transport/transport.service.ts`

- Declares `export type ApiTransport = 'rest' | 'graphql';`
- Exposes `readonly active: Signal<ApiTransport>` backed by a private `WritableSignal`, plus a `use(transport: ApiTransport): void` method. Never expose the writable signal directly.
- Initial value resolution order: `localStorage['api-transport']` if it holds a valid `ApiTransport` → otherwise `environment.defaultTransport` → otherwise `'rest'`. Reject and discard any other stored value rather than trusting it.
- `use()` writes through to `localStorage` so the choice survives a reload, and emits a structured console/telemetry entry recording the change so benchmark runs are traceable.
- Provided in root. It is the only writable owner of the active transport in the entire application.

**`TransportSelectorComponent`** — `src/app/shared/components/transport-selector/`

- Standalone, `ChangeDetectionStrategy.OnPush`, injects `TransportService` via `inject()`.
- Renders a labelled `<select>` (or the design system's equivalent) with exactly two options, `REST` and `GraphQL`, bound to `transport.active()`; the change handler calls `transport.use(value)`.
- Accessibility is mandatory: a visible or `aria-label`ed label ("API transport"), keyboard operable, and an `aria-live="polite"` status region announcing the new transport after a change.
- Carries `data-testid="transport-selector"` so Playwright can drive the same suite under both transports.
- Contains no business logic, no HTTP, and no knowledge of any gateway — it only reads and writes the service.

**Placement and visibility**

- Rendered once in the shell toolbar component, next to the user menu — never inside a feature component, and never more than once.
- Visible in `development` and in the dedicated `benchmark` environment. In `production` it is hidden and the transport is pinned to `environment.defaultTransport`, so the experiment tooling cannot leak into a real deployment.

**Reactive refetch**

- Screens must re-issue their queries when the selection changes. Include `transport.active()` in the `params` of the `rxResource`/`resource` (or in the derived signal driving the request) for every data-loading screen, so flipping the dropdown immediately re-runs the current view over the other transport with no manual reload.
- On change, clear the Apollo cache so a GraphQL run never serves data fetched during a REST run, and vice versa. Measurements must never be contaminated by a stale cache.

**Tests**

- Unit tests: `TransportService` defaults correctly, persists to `localStorage`, and ignores corrupt stored values; `TransportSelectorComponent` renders both options, reflects the current value, and calls `use()` on change.
- Component test: a data-loading screen re-fetches when the transport signal changes.
- Playwright: every end-to-end journey is parameterised over both transports by driving `data-testid="transport-selector"`.

## Quality Bar

- Backend line coverage ≥ 80%; integration tests run against real SQL Server via Testcontainers.
- Tests must cover async paths, failure paths, authorization denials, validation failures, and edge cases — not just happy paths.
- Frontend: component tests with Jest + Angular Testing Library, end-to-end flows with Playwright run against both transports, accessibility assertions on key screens.
- Every gateway contract verified against both its REST and its GraphQL implementation by the same shared test suite.
- Security: OWASP Top 10 addressed explicitly — parameterized queries via EF Core, input validation at every boundary, no secrets in source, HTTPS enforced, least-privilege authorization, rate limiting on auth endpoints.
- Public C# APIs carry XML docs; public TS APIs carry JSDoc.
- `README.md` documents purpose, prerequisites, setup, architecture, how to run both API surfaces, and the full npm and dotnet command reference.

## Working Method

1. Read `command.txt` and any existing code before proposing or making changes.
2. Work in vertical slices: pick one feature, implement it end-to-end (domain → application → infrastructure → REST → GraphQL → Angular gateway pair → UI → tests), verify it builds and its tests pass, then move to the next.
3. After each slice, actually run `dotnet build`, `dotnet test`, `npm run build`, and `npm test`, then fix what breaks. Do not report success without evidence.
4. When a design decision has real trade-offs, state the alternatives in one or two sentences, pick one, justify it, and implement it. Do not stall on the choice.
5. Keep changes scoped to what was asked. No speculative abstractions, no gratuitous refactors of untouched code.

## Suggested Build Order

1. Solution scaffolding, project references, EditorConfig, nullable + strict analyzers, Serilog, health checks
2. Domain model and domain unit tests
3. EF Core `DbContext`, configurations, initial migration, seed data
4. Application layer: mediator abstraction, pipeline behaviours, item commands/queries, FluentValidation validators
5. Identity, JWT issuance and refresh, policy-based authorization
6. REST surface + Swagger + integration tests
7. GraphQL surface + DataLoaders + schema tests, verified for parity with REST
8. Angular workspace generated with `ng new`, npm scripts wired up: app shell, routing, auth flow, functional guards, interceptors, layout, global error handling, loading states
9. Gateway abstraction layer + `TransportService` + `TransportSelectorComponent` in the toolbar + both REST and GraphQL implementations wired through the delegating gateway router
10. Item authoring UI: list with search/filter/sort/pagination, editors for all four item types, preview
11. Exam builder UI: composition, reorder with CDK drag-drop, preview
12. Administration UI: users, roles
13. Instrumentation and benchmark harness capturing response time, payload size, and request counts for both surfaces
14. Containerization, environment configuration, CI pipeline

## Hard Rules

- Never oversimplify; assume university professors and senior engineers will read this code.
- Never ship low-quality code, dead code, or commented-out code.
- Never break the dependency rule of Clean Architecture.
- Never duplicate business logic between the REST and GraphQL surfaces.
- Never let a component, route, or form know which transport is active; it must depend only on the abstract gateway.
- Never commit credentials, connection strings with passwords, or signing keys.
- Prefer production-ready solutions over demo shortcuts, always.
