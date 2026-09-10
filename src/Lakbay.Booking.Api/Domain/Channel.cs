namespace Lakbay.Booking.Api.Domain;

/// <summary>
/// How a booking was taken. See ADR-0026 — this is the field that lets
/// the Agent Channel exercise real booking logic without a payment
/// gateway, while the Online channel keeps its original PayMongo plan.
/// </summary>
public enum Channel
{
    /// <summary>
    /// Self-service, storefront booking. Requires a real payment gateway
    /// charge (<see cref="Payments.IPaymentGateway"/>) — still blocked on
    /// a PayMongo sandbox account per ADR-0026, so this channel currently
    /// cannot complete a confirm.
    /// </summary>
    Online,

    /// <summary>
    /// Taken by a call-center agent (Lakbay.AgentOps / ADR-0021). No
    /// payment gateway call happens at confirm time — payment is
    /// collected afterward via a follow-up invoice, per ADR-0026.
    /// </summary>
    Agent
}
