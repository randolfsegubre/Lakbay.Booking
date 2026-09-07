using Lakbay.Booking.Api.Domain;
using Lakbay.Booking.Api.Infrastructure;
using Lakbay.Booking.Api.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lakbay.Booking.Api.Application.Commands;

/// <summary>
/// The correctness-critical handler in this repo. Implements ADR-0011
/// (a single atomic, conditional UPDATE — never a SELECT-then-UPDATE) and
/// ADR-0026 (Channel decides payment handling only, never whether the
/// decrement happens or how).
/// </summary>
public sealed class ConfirmBookingCommandHandler(
    BookingDbContext db,
    IPaymentGateway paymentGateway) : IRequestHandler<ConfirmBookingCommand, ConfirmBookingResult>
{
    public async Task<ConfirmBookingResult> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // ADR-0011: the availability check and the decrement are the SAME
        // statement. SQL Server's row-level locking on this UPDATE is what
        // makes two simultaneous confirms for the same last slot safe —
        // the second one's UPDATE physically cannot proceed until the
        // first transaction commits or rolls back.
        var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE AvailabilitySlots
               SET AvailableCount = AvailableCount - 1
               WHERE ProductId = {request.ProductId}
                 AND DateSlot = {request.DateSlot}
                 AND AvailableCount > 0",
            cancellationToken);

        if (rowsAffected == 0)
        {
            // 0 rows affected: nothing was available. No partial state was
            // ever written, so there is nothing to roll back except the
            // (empty) transaction itself.
            await transaction.RollbackAsync(cancellationToken);
            return ConfirmBookingResult.NotAvailable();
        }

        PaymentStatus paymentStatus;

        if (request.Channel == Channel.Agent)
        {
            // ADR-0026: Agent channel never calls the gateway at all.
            // Lakbay.AgentOps' Hangfire jobs (ADR-0025) send the
            // follow-up invoice/payment-collection link asynchronously.
            paymentStatus = PaymentStatus.PendingInvoice;
        }
        else
        {
            try
            {
                // Online channel: still routes through the real gateway
                // interface. Currently a stub (PayMongo sandbox account
                // doesn't exist yet, ADR-0026) — this deliberately throws
                // rather than pretending to succeed.
                var chargeResult = await paymentGateway.ChargeAsync(
                    new PaymentChargeRequest(request.ProductId, request.CustomerId, AmountPhp: 0m),
                    cancellationToken);

                paymentStatus = chargeResult.Succeeded ? PaymentStatus.Paid : PaymentStatus.Failed;
            }
            catch (NotImplementedException ex)
            {
                // The slot was provisionally decremented above as part of
                // this same transaction — since payment could not be
                // taken, release it by rolling back everything, not just
                // skipping the booking insert.
                await transaction.RollbackAsync(cancellationToken);
                return ConfirmBookingResult.PaymentGatewayNotImplemented(ex.Message);
            }
        }

        var booking = BookingReservation.Confirm(request.ProductId, request.DateSlot, request.CustomerId, request.Channel, paymentStatus);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ConfirmBookingResult.Confirmed(booking.Id, paymentStatus);
    }
}
