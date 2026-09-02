# Lakbay.Booking — Developer Handbook

Written the moment local setup actually worked (2026-09-06). The test for
this document: could you follow it on a plane, no internet, no AI agent?

## Layout

```
Lakbay.Booking.sln
src/Lakbay.Booking.Api/       ASP.NET Core minimal API (net10.0)
tests/Lakbay.Booking.Tests/   xUnit, references the Api project directly
                               via WebApplicationFactory<Program>
```

References `../Lakbay.Contracts/csharp/Lakbay.Contracts.csproj` directly
(project reference, not a package) — see that repo's handbook for why.

## Local setup — proven working, 2026-09-06

Requires: .NET SDK 10.x.

```bash
dotnet build     # 0 Warning(s), 0 Error(s)
dotnet test      # 1 passed — the /health endpoint boots
dotnet run --project src/Lakbay.Booking.Api
# GET http://localhost:<port>/health → { "status": "ok", "service": "Lakbay.Booking" }
```

No database, no Docker needed for this much — Phase 0 only proves the
service boots. Real scope (CQRS command/query handlers per ADR-0002, the
atomic availability decrement per ADR-0011, PayMongo integration) is
Phase 4, and needs SQL Server (ADR-0005) once that's set up.

## Adding a new MediatR handler — worked walkthrough (once Phase 4 starts)

Not applicable yet — no MediatR reference exists in this project as of
Phase 0 on purpose (nothing to wire it into yet; see the note in
`CLAUDE.md` about not adding dependencies before they're used). This
section will be filled in with a real, proven walkthrough the moment
Phase 4 adds the first handler — not written speculatively now.
