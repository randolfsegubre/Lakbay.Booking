# Lakbay.Booking — Start Here

This file is intentionally short. It exists so any Claude Code session (or
other AI coding assistant) rooted here auto-loads it and is pointed at the
real documentation before touching anything.

**Read, in this order, before writing any code:**

1. [../Lakbay.Docs/docs/01_CLAUDE.md](../Lakbay.Docs/docs/01_CLAUDE.md) —
   the platform AI operating manual. Constitution for the whole Lakbay
   estate; if anything else conflicts with it, it wins unless the user
   explicitly overrides it in the current conversation.
2. [../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
   — **Phase 0** (foundation/scaffolding, xUnit test project scaffolded
   alongside the main project from the start) and **Phase 4**
   (booking & payments — this repo's real work) are this repo's phases.
3. [../Lakbay.Docs/docs/04_TASKS.md](../Lakbay.Docs/docs/04_TASKS.md) —
   current status across the whole platform.
4. Decisions this repo must honor:
   [ADR-0002](../Lakbay.Docs/docs/adr/ADR-0002-cqrs-booking.md) (CQRS via
   MediatR, not a shared service class — read this before writing a
   single handler) and
   [ADR-0003](../Lakbay.Docs/docs/adr/ADR-0003-separate-booking-data.md)
   (why this repo owns its own database instead of sharing Umbraco's).
5. [../Lakbay.Docs/docs/03_ARCHITECTURE_AND_PATTERNS_GUIDE.md](../Lakbay.Docs/docs/03_ARCHITECTURE_AND_PATTERNS_GUIDE.md)
   — OOP/SOLID/pattern tables this repo's code should follow (Strategy
   pattern for `IPaymentGateway`/`INotificationChannel`, domain events for
   side effects, Dependency Inversion at the composition root).

## What this repo is

.NET minimal API. Owns orders, baskets, the availability calendar, and
payment orchestration (PayMongo first, per the Blueprint's stack
decisions) — in its **own** Azure SQL database, separate from
`Lakbay.Cms`. Talks to `Lakbay.Cms`/`Lakbay.Web` over Azure Service Bus
events (`BookingConfirmed`, `AvailabilityChanged`), never by querying
another service's database directly.

**Non-negotiable from ADR-0002:** commands and queries are separate
MediatR handlers. If you catch yourself writing a `BookingService` class
with both read and write methods on it, stop and re-read the ADR — that
exact shape is what made `E-Commerse.AI.API`'s controllers unmaintainable
stubs.

## Local setup

Not yet proven — Phase 0 is not complete. Once the minimal API + xUnit
project boots locally, the exact commands go in
`Docs/DEVELOPER_HANDBOOK.md` (create that file the moment setup actually
works, not from memory afterward).

**Database:** SQL Server (Docker, Developer Edition) locally — never
Azure SQL Database, which has no local/offline edition. Azure SQL
Database is only used once a live/staging environment exists. See
[ADR-0005](../Lakbay.Docs/docs/adr/ADR-0005-local-sql-server-not-azure-sql.md).

## End of session

Update `../Lakbay.Docs/docs/04_TASKS.md` and append an entry to
`../Lakbay.Docs/docs/05_DEVLOG.md` for anything that changed phase status
or made a new structural decision. A new structural decision gets its own
ADR under `../Lakbay.Docs/docs/adr/`.
