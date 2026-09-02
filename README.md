# Lakbay.Booking

Orders, basket, availability calendar, and payment orchestration for the
Lakbay platform — deliberately separate from `Lakbay.Cms`. See
[ADR-0003](../Lakbay.Docs/docs/adr/ADR-0003-separate-booking-data.md) for
why, and [ADR-0002](../Lakbay.Docs/docs/adr/ADR-0002-cqrs-booking.md) for
the CQRS shape this repo's code follows.

Phase 0 scaffolding is done (minimal API + xUnit, boots and tests green)
— see [Docs/DEVELOPER_HANDBOOK.md](Docs/DEVELOPER_HANDBOOK.md) for proven
local setup, and [CLAUDE.md](CLAUDE.md) /
[../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
for what's next (Phase 4 — real CQRS handlers and the atomic
availability decrement).
