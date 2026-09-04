# Transaction Management

A master–detail transaction management application built with ASP.NET Core 9 MVC, EF Core 9 and
SQL Server, organised as a Clean Architecture solution with CQRS, the repository and unit-of-work
patterns, FluentValidation, centralised exception handling and an RDLC report.

---

## 1. Project Overview

The application manages transactions in the classic master–detail shape:

* a **master** record — transaction number, date, customer/supplier, reference, remarks, totals;
* many **detail** lines — product, date, description, quantity, amount and an active flag.

From one screen a user can add, edit and remove detail rows and save everything with the master in
a single atomic operation. On save the server works out, on its own, which submitted lines are new,
which changed, which were removed and which are untouched — it never trusts the browser to say so.

Functionality:

| Area | What it does |
| --- | --- |
| List | Server-side pagination, search, sortable columns, per-row actions |
| Create | Master fields plus a dynamic detail grid; at least one active line required |
| Edit | Loads the whole aggregate; add/modify/delete rows; optimistic concurrency |
| Details | Read-only view of master, details and totals |
| Delete | Removes the aggregate; detail rows cascade |
| Report | RDLC report rendered to PDF (also Excel and Word), printable |

---

## 2. Architecture

```
TransactionManagement.sln
│
├── src/
│   ├── TransactionManagement.Domain          entities, invariants, value objects (no dependencies)
│   ├── TransactionManagement.Application     CQRS, DTOs, validators, abstractions
│   ├── TransactionManagement.Infrastructure  EF Core, repositories, unit of work, report data
│   └── TransactionManagement.Web             MVC controllers, ViewModels, Razor views, RDLC
│
├── tests/
│   ├── TransactionManagement.UnitTests
│   └── TransactionManagement.IntegrationTests
│
└── database/
    ├── 01-schema.sql
    └── 02-sample-data.sql
```

Dependency direction:

```
Web ─────────┐
             ├──► Application ──► Domain
Infrastructure┘
```

The Domain project has **zero** package references. It knows nothing about EF Core, SQL Server,
ASP.NET Core, Razor or RDLC. Infrastructure and Web both depend inwards; nothing depends outwards.

Key structural decisions:

* **`Transaction` is the aggregate root.** `TransactionDetail` has no repository and is not exposed
  as a `DbSet`. Detail lines are only reachable through their transaction, in the model and in the
  persistence layer alike.
* **The aggregate owns the reconciliation logic.** `Transaction.ApplyDetails` decides what is new,
  modified, removed or unchanged. The command handler orchestrates; it does not decide.
* **Reads and writes are separated.** Commands load aggregates and apply behaviour; queries project
  straight into DTOs with `AsNoTracking()` and never materialise an entity.

---

## 3. Technologies

| Concern | Choice |
| --- | --- |
| Framework | ASP.NET Core 9 MVC (Razor views) |
| Language | C# 13, `net9.0` throughout |
| ORM | Entity Framework Core 9 (SQL Server provider) |
| Database | Microsoft SQL Server 2019+ / LocalDB |
| Mediator / CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Reporting | RDLC via `ReportViewerCore.NETCore` |
| Testing | xUnit, FluentAssertions, NSubstitute, EF Core SQLite (integration) |
| UI | Bootstrap 5 + a single theme stylesheet, vanilla JavaScript (no SPA framework) |

---

## 4. Prerequisites

* .NET 9 SDK
* SQL Server 2019+, SQL Server Express, or LocalDB (ships with Visual Studio)
* Windows is **not** required by the framework targets. Report rendering has been verified on
  Windows; the PDF renderer touches `System.Drawing`, so other platforms may need additional font
  packages.
* Visual Studio 2022 (17.12+) or JetBrains Rider, optional

---

## 5. Database Setup

Two supported paths — pick one, not both, for a given database.

**A. EF Core migrations (recommended for development)**

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
    --project src/TransactionManagement.Infrastructure \
    --startup-project src/TransactionManagement.Web
dotnet ef database update \
    --project src/TransactionManagement.Infrastructure \
    --startup-project src/TransactionManagement.Web
```

In Development the application also applies pending migrations and seeds sample data on startup
(see section 7).

**B. SQL scripts**

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -i database/01-schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -i database/02-sample-data.sql
```

`01-schema.sql` creates the database, tables, primary keys, foreign keys, unique constraints, check
constraints and indexes. `02-sample-data.sql` inserts master data and 45 sample transactions.
Neither script contains a stored procedure.

If you use the scripts, turn startup initialisation off:

```json
"DatabaseInitialisation": { "ApplyMigrations": false, "SeedSampleData": false }
```

---

## 6. Connection String Configuration

The default lives in `src/TransactionManagement.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TransactionManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

That default uses integrated security and therefore contains **no credentials**. If your
environment needs a username and password, do not put them in a tracked file — use user secrets or
an environment variable:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;User Id=...;Password=..." \
    --project src/TransactionManagement.Web

# or
setx ConnectionStrings__DefaultConnection "Server=...;User Id=...;Password=..."
```

The application fails fast with a clear message if the connection string is missing.

---

## 7. Migration / Database Instructions

Startup initialisation is controlled by configuration so it can never fire unintentionally against
a production database:

```json
"DatabaseInitialisation": {
  "ApplyMigrations": true,
  "SeedSampleData": true
}
```

Both default to `true` in Development and `false` elsewhere. Seeding is idempotent — it inserts
only into empty tables, so restarting never duplicates data.

---

## 8. Application Setup

```bash
git clone <repository-url>
cd TransactionManagement
dotnet restore
dotnet build
```

---

## 9. Running the Application

```bash
dotnet run --project src/TransactionManagement.Web
```

Then open `https://localhost:7231`. The root route redirects to the transaction list.

Run the tests with:

```bash
dotnet test
```

---

## 10. Master–Detail Implementation

This is the core of the assignment, so it is worth being precise about where each decision lives.

**On the client.** Each detail row emits a hidden `Details.Index` field carrying an opaque key, and
every input in that row is named `Details[key].Field`. ASP.NET Core's model binder uses
`Details.Index` to discover the keys, which means rows can be added and removed in any order
without renumbering the survivors — deleting a middle row and saving simply works. Adding and
removing rows and the running totals are the *only* things the JavaScript does; it enforces no rule.

**On the server.** `UpdateTransactionCommandHandler` loads the tracked aggregate and hands the
submitted lines to `Transaction.ApplyDetails`, which classifies each one:

| Submitted `Id` | Found on this aggregate? | Outcome |
| --- | --- | --- |
| `0` | — | **New** — appended via `AddDetail` |
| non-zero | yes, values differ | **Modified** — updated in place |
| non-zero | yes, values identical | **Unchanged** — left alone, no UPDATE emitted |
| (absent) | line exists but was not submitted | **Deleted** — orphaned and removed |
| non-zero | **no** | **Rejected** — `BusinessRuleViolationException` |

That last row is the security-relevant one: a detail identifier arriving from the browser is a
*claim*, never an authority. `FindOwnedDetail` checks it against the aggregate's own collection, so
a tampered identifier cannot reach another transaction's data.

`ApplyDetails` returns a `DetailReconciliationResult` (added / modified / removed / unchanged) which
the handler writes to the log, so what a save actually changed is visible after the fact.

Everything is then persisted by **one** `SaveChangesAsync`. EF Core wraps that batch in a database
transaction, so the master update, the inserts, the updates and the deletes commit or roll back
together. No partial save is possible.

---

## 11. CQRS Implementation

```
MVC Controller
      ↓  ISender.Send()
LoggingBehavior → PerformanceBehavior → ValidationBehavior
      ↓
Command / Query Handler
      ↓
Repository → EF Core → SQL Server
```

Commands: `CreateTransactionCommand`, `UpdateTransactionCommand`, `DeleteTransactionCommand`.
Queries: `GetTransactionsQuery`, `GetTransactionByIdQuery`, `GetTransactionReportQuery`,
`GetBusinessPartnerLookupQuery`, `GetProductLookupQuery`.

Each has exactly one handler with one responsibility. There is deliberately no
`TransactionService` god class.

Query handlers use `AsNoTracking()` and project with `Select(...)` directly into DTOs — no
`Include`, no object graph loaded to then throw most of it away.

---

## 12. Repository and Unit of Work

`IRepository<TEntity>` carries the persistence behaviour that is genuinely common to every
aggregate. Aggregate-specific access lives on focused interfaces instead of being forced through
the generic one:

* `ITransactionRepository` — `GetWithDetailsAsync`, `TransactionNumberExistsAsync`,
  `SetOriginalRowVersion`
* `IBusinessPartnerRepository` — `ExistsAndIsActiveAsync`
* `IProductRepository` — `GetExistingActiveIdsAsync` (set-based, so detail validation is one round
  trip rather than one query per line)

`IUnitOfWork` exposes `SaveChangesAsync` and `BeginTransactionAsync`. It returns
`IDatabaseTransaction` — a small provider-agnostic abstraction — rather than EF Core's
`IDbContextTransaction`, so the Application layer does not take a dependency on a persistence
technology it otherwise has no use for. `EfCoreDatabaseTransaction` adapts one to the other.

Repositories never call `SaveChanges`. Committing is the unit of work's job, and each command
performs exactly one logical unit of work.

---

## 13. Validation

Validation happens in three layers, each with a different job:

1. **Client-side** — HTML5 attributes and jQuery unobtrusive validation. Convenience only.
2. **FluentValidation** — runs in `ValidationBehavior` for every command, before the handler.
   Shape, ranges, lengths, "at least one active line", no duplicate line identifiers.
3. **Domain invariants** — enforced by the aggregate itself and impossible to bypass: quantity > 0,
   amount ≥ 0, transaction date not in the future, detail date not after the transaction date and
   not more than 90 days before it, at least one active line, detail ownership.

Validators: `CreateTransactionCommandValidator`, `CreateTransactionDetailCommandValidator`,
`UpdateTransactionCommandValidator`, `UpdateTransactionDetailCommandValidator`,
`DeleteTransactionCommandValidator`, plus validators for the by-id queries.

`ValidationException` is caught at the controller boundary and copied into `ModelState` so the form
redisplays with the server's messages. That is the only `catch` in the controllers, and it exists
for presentation, not for error suppression.

---

## 14. Exception Handling

`GlobalExceptionHandler` implements `IExceptionHandler` and is the single place where an exception
becomes a status code, a log entry and a user-facing message:

| Exception | Status | Shown to the user |
| --- | --- | --- |
| `EntityNotFoundException` | 404 | "… was not found." |
| `ConcurrencyConflictException` | 409 | "Changed by another user — please reload." |
| `BusinessRuleViolationException` | 400 | The domain rule message |
| `ValidationException` | 400 | "Please review the highlighted fields." |
| `OperationCanceledException` | 499 | "The request was cancelled." |
| anything else | 500 | A generic message; the detail goes to the log only |

Unexpected exceptions are logged with full context (`LogError`); expected ones are logged at
warning level. **No** SQL text, stack trace, connection string or internal detail ever reaches the
browser. There is no `try { } catch (Exception) { }` in any controller action.

---

## 15. RDLC Reporting

```
Transaction List / Details
        ↓ Report action
GetTransactionReportQuery
        ↓
GetTransactionReportQueryHandler
        ↓
ITransactionReportService  (Application abstraction)
        ↓
TransactionReportService   (Infrastructure — one read-only projection)
        ↓
TransactionReportDto + TransactionReportLineDto
        ↓
IRdlcReportRenderer → TransactionReport.rdlc → PDF / Excel / Word
```

`Reports/TransactionReport.rdlc` contains a company header, report title, transaction number, date,
customer/supplier, reference and remarks; a bordered detail table (line no., product, date,
description, quantity, amount) whose header repeats on every page; a totals row summing **active**
lines only; and a page footer with the print timestamp and "Page N of M". Page setup is A4 portrait
with 0.5" margins, so it prints cleanly as-is.

The renderer lives in the Web project because rendering is a presentation concern: the Application
layer defines *what* the report contains, the Web layer decides how it is turned into bytes.

**On the reporting package.** `ReportViewerCore.NETCore` is a recompilation of Microsoft's
ReportViewer for .NET Core in which report-expression compilation was moved from CodeDom to Roslyn.
That is the reason it is used here rather than the more commonly cited `AspNetCore.Reporting`: that
package derives its compiler path from the runtime version and looks for a .NET Framework compiler
at `…\Framework64\v9.0.19\vbc.exe`, which does not exist on .NET 9. Since every RDLC contains
expressions — a plain `=Fields!Quantity.Value` is one — no report can render through it at all on
this runtime.

Open it from the list (**Report**) or from the details page (**View / Print report**). PDF opens
inline in the browser's viewer; Excel and Word download.

---

## 16. Pagination

`GetTransactionsQuery` carries `PageNumber`, `PageSize`, `SearchTerm`, `SortBy` and
`SortDescending`. The handler:

* clamps page size to **10–100** and page number to at least 1;
* runs `CountAsync()` on the filtered query for the total;
* re-clamps the page number if a search left fewer pages than requested;
* orders, then `Skip`/`Take`, then projects — so paging happens in SQL, never in memory.

Sorting is expressed as an **enum**, not a column-name string, so no user input can influence the
generated SQL. `IX_Transactions_TransactionDate` and `IX_Transactions_BusinessPartnerId` back the
common orderings and filters.

---

## 17. Security Considerations

* **Anti-forgery** — `AutoValidateAntiforgeryTokenAttribute` is registered globally, so every
  unsafe verb is protected whether or not the action remembers the attribute.
* **Over-posting** — dedicated ViewModels per screen. `TransactionCreateViewModel` has no `Id`, no
  transaction number and no concurrency token, because those are server-owned. Display-only
  properties are marked `[BindNever]`.
* **No entity binding** — domain entities are never model-bound and never reach a Razor view.
* **Server-authoritative identifiers** — the transaction number is issued by
  `ITransactionNumberGenerator`; detail ownership is verified inside the aggregate; the route id
  must match the posted id on edit.
* **Parameterised SQL** — everything goes through EF Core LINQ. There is no raw SQL anywhere.
* **Concurrency** — a SQL Server `rowversion` on the aggregate; a stale save is refused, never
  silently applied.
* **Logging hygiene** — no passwords, connection strings, tokens or request payloads are logged.
* **Error hygiene** — see section 14.
* **Transport** — HTTPS redirection always; HSTS outside Development.

Authentication and authorisation are intentionally **not** implemented — see section 19.

---

## 18. Assumptions

1. **Single business partner table.** Customers and suppliers share `BusinessPartners` with a
   `PartnerType` discriminator, which keeps `Transaction` to one foreign key.
2. **Totals cover active lines only.** `IsActive` is a real business flag: an inactive line is kept
   for reference but excluded from `TotalQuantity` and `TotalAmount`. This is what gives the
   checkbox in the grid actual meaning.
3. **Amount is entered, not derived.** The grid captures quantity and amount independently, as the
   specification describes; `Product.DefaultUnitPrice` is a convenience for data entry only.
4. **Transaction numbers are `TRX-yyyyMM-#####`,** sequential within a calendar month, issued
   server-side and immutable once assigned. The generator's read-then-increment is not atomic under
   concurrent creates; the unique index `UX_Transactions_TransactionNumber` is the authority that
   makes a duplicate impossible.
5. **Detail dates are bounded** — not after the transaction date, not more than 90 days before it.
6. **EF Core is referenced by the Application project.** Only for the asynchronous LINQ operators
   (`AsNoTracking`, `ToListAsync`, `CountAsync`) used by read-side handlers over `IQueryable`. No
   `DbContext`, provider or migration type is referenced there. This is the common pragmatic
   trade-off in Clean Architecture; the alternative — a hand-rolled async query abstraction — buys
   purity at the cost of a layer of indirection with no other purpose. The **Domain** project
   remains completely free of EF Core, which is the constraint that actually matters.
7. **Hard delete** (section 26 of the specification asks for a deliberate choice). Transactions and
   their details are hard-deleted, because the assignment states no audit or history requirement
   and soft delete everywhere would add a global query filter, a nullable column on every table and
   a permanent source of "why is this row missing" bugs for no stated benefit. Should audit history
   become a requirement, the change is localised: an `IsDeleted` flag plus a query filter on the
   `Transaction` aggregate only.
8. **Timestamps are UTC** (`CreatedAtUtc`, `ModifiedAtUtc`), maintained by a save interceptor.
   Business dates (`TransactionDate`, `DetailDate`) are dates without a time component.

---

## 19. Limitations

* **No authentication or authorisation.** The security work that *is* present is the request-level
  hardening listed in section 17. Adding ASP.NET Core Identity with role-based policies on the
  `Transactions` actions would be the natural next step; nothing in the design blocks it.
* **Report rendering is verified on Windows only.** The target frameworks are platform-neutral and
  the PDF renderer is expected to work elsewhere, but font handling on Linux has not been tested.
* **No migration is committed.** The first `dotnet ef migrations add` generates it, or use the SQL
  scripts. This keeps the repository free of a migration that would conflict with the scripts.
* **FluentValidation error keys and grid row keys do not always line up.** Because detail rows use
  opaque keys rather than sequential indices, a server-side failure on a specific row surfaces in
  the validation summary rather than next to that exact input. The message is precise; its
  placement is not.
* **No output caching or distributed cache.** Lookup lists are queried per request. At this data
  volume that is the right trade; at scale they would be cached.
* **Integration tests run on SQLite,** which has no `rowversion`. The concurrency mapping is relaxed
  for those tests only; the rest of the model — relationships, cascades, precision, indexes, check
  constraints — is exercised exactly as configured for production. Concurrency itself is covered by
  unit tests and by the SQL Server mapping.

---

## 20. Future Improvements

1. Authentication and role-based authorisation (Admin / Operator), with per-action policies.
2. A committed initial migration, plus a CI step that verifies the model has no pending changes.
3. Domain events raised by the aggregate (`TransactionCreated`, `TransactionUpdated`) dispatched
   after commit, for audit trails and integrations.
4. An audit table capturing who changed what, once a user identity exists to record.
5. Cached lookup queries with invalidation on master-data change.
6. `Result<T>` returns for expected business failures, keeping exceptions for the genuinely
   exceptional.
7. More report formats and a report parameter screen (date range, partner, product).
8. Bulk operations for detail lines (paste from spreadsheet, import CSV).
9. Playwright end-to-end tests covering the add/edit/delete-row flow in a real browser.
10. Health checks and structured logging to a sink such as Seq or Application Insights.

---

## User Interface

The application shell is a fixed navy sidebar, a white topbar, and a gradient page-header card
carrying the title, breadcrumb and the page's actions — the same shape on every screen.

**All styling lives in `wwwroot/css/site.css`.** No Razor view contains a `style` attribute or a
`<style>` block, and no JavaScript writes to `element.style`; the scripts toggle classes and let the
stylesheet decide what that means. The file is organised in numbered sections (design tokens, shell,
sidebar, topbar, page header, panels, data table, pagination, forms, invoice layout, summary panel,
action bar, detail grid, error page, utilities, responsive) and every colour, radius, shadow and
dimension comes from a custom property on `:root`, so re-theming is a matter of editing the tokens.

Bootstrap 5 supplies the grid, form controls and utility classes; `site.css` layers the theme on
top rather than restating it. Icons are Bootstrap Icons.

The create and edit screens use the invoice layout: master fields and a product quick-search on the
left above the detail grid, a sticky Summary panel on the right, and a fixed action bar along the
bottom showing total items, total quantity and grand total next to the save button. The quick-search
reads its product list out of the detail-row `<template>` already in the page, so the catalogue is
serialised into the markup exactly once and the two can never drift apart.

### List controls

The transaction list carries the familiar "Show N entries" / "Search:" controls above the grid and
`Previous 1 2 3 4 5 … 1105 Next` below it. **No client-side table plug-in is used.** The controls are
a plain GET form: changing the length or typing in the search box re-submits it (debounced), and the
query still executes in SQL over the whole result set. A browser-side table library would have to
hold every row in memory to filter or sort, which is exactly what section 16 is designed to avoid.

The pager renders a five-page window plus an ellipsis and the last page, so a 1,105-page result
produces the same short control as a 3-page one.

### The transaction document

`Transactions/Details/{id}` is laid out as a printable document rather than a data screen:
letterhead with logo, company name and address; document type, number, date and reference; a
bill-to / supplier block; the line-item table; totals with the amount spelled out in words; and
signature lines.

The letterhead is configuration, not markup — the `CompanySettings` section of `appsettings.json`
carries the name, tagline, address, contact details, logo path, currency symbol and currency name,
and the same section feeds the RDLC report header, so the screen and the report cannot disagree
about who published the document. The bundled `wwwroot/img/logo.svg` is a placeholder mark; replace
the file or point `LogoPath` elsewhere.

The print stylesheet drops the sidebar, topbar and every action, sets A4 with 12 mm margins, and
marks rows, the totals block and the signature row `break-inside: avoid`, so **Print** in the browser
produces the same document as the RDLC PDF without any separate print view.

`Reports/TransactionReport.rdlc` reproduces that same document: embedded logo, letterhead,
`TRANSACTION` title with number/date/reference, the parties row, the blue line table, remarks and
amount in words, the totals box and the signature lines. Two things keep the two in step — the
letterhead comes from `CompanySettings` on both sides, and every value the report prints is
formatted in `RdlcReportRenderer` and passed in as a report parameter rather than being computed in
an RDLC expression. Adding a field to the report therefore means adding a parameter in one C# method,
not editing expressions in two places.

The logo is embedded in the definition as base64 PNG (`EmbeddedImages`), because RDLC cannot consume
the SVG the screen uses. `wwwroot/img/logo.svg` and that embedded copy are the same mark; replacing
the logo means replacing both.

### Responsive behaviour

Every screen is usable from roughly 320px upward. Below 992px the sidebar becomes an overlay with a
backdrop; below 768px both tables — the read-only list and the editable detail grid — stop being
tables and become stacked cards, each cell taking its heading from its own `data-label` attribute
rather than from a header row that is no longer visible; below 576px the pager keeps only Previous,
Next and the current page.

Print styles hide the shell, so any screen prints as a clean document.

---

## Where to look first

| To understand… | Read |
| --- | --- |
| The master–detail algorithm | `Domain/Entities/Transaction.cs` → `ApplyDetails` |
| How a save is orchestrated | `Application/Transactions/Commands/UpdateTransaction/` |
| How rows bind without renumbering | `Web/Views/Transactions/_DetailGrid.cshtml` |
| How errors reach the user | `Web/Infrastructure/GlobalExceptionHandler.cs` |
| How the report is built | `Infrastructure/Reporting/TransactionReportService.cs` |
| That the reconciliation is correct | `UnitTests/Domain/TransactionDetailReconciliationTests.cs` |
| Any styling question | `Web/wwwroot/css/site.css` — the only stylesheet; no view contains an inline style |
