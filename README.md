# Lakbay.Booking

Orders, basket, availability calendar, and payment orchestration for the
Lakbay platform — deliberately separate from `Lakbay.Cms`. See
[ADR-0003](../Lakbay.Docs/docs/adr/ADR-0003-separate-booking-data.md) for
why, and [ADR-0002](../Lakbay.Docs/docs/adr/ADR-0002-cqrs-booking.md) for
the CQRS shape this repo's code follows.

Real CQRS handlers, the atomic availability decrement (ADR-0011), and a
gRPC confirm-booking service for the Agent Channel (ADR-0027) are all
built and verified live — see [Docs/DEVELOPER_HANDBOOK.md](Docs/DEVELOPER_HANDBOOK.md)
for local setup, and [CLAUDE.md](CLAUDE.md) /
[../Lakbay.Docs/docs/02_BUILD_PLAN.md](../Lakbay.Docs/docs/02_BUILD_PLAN.md)
for full phase status.

## E2E testing

`dotnet build` clean, `dotnet test` 11/11 passing (SQL Server LocalDB, no
Docker needed), including a real gRPC integration test
(`BookingConfirmGrpcServiceTests.cs`) against a `WebApplicationFactory`-hosted
service. Live cross-process verification (both `Lakbay.Booking` and
`Lakbay.AgentOps` actually running, real network calls, not the in-process
test host): a real confirm-booking call from `Lakbay.AgentOps` over gRPC
succeeded and returned a real booking id; a second call against the same
now-sold-out slot was correctly rejected, proving the atomic decrement
holds through gRPC, not just REST. Full trail:
[`../Lakbay.Docs/docs/05_DEVLOG.md`](../Lakbay.Docs/docs/05_DEVLOG.md)'s
2026-09-12 entries, [ADR-0027](../Lakbay.Docs/docs/adr/ADR-0027-agent-channel-confirm-booking-over-grpc.md).
