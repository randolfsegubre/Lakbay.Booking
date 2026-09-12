using Grpc.Core;
using Lakbay.Booking.Api.Application.Commands;
using Lakbay.Booking.Api.Domain;
using MediatR;

namespace Lakbay.Booking.Api.Grpc;

/// <summary>
/// ADR-0027: the Agent Channel's synchronous confirm-booking call. A thin
/// adapter only — it maps the incoming protobuf message to the same
/// <see cref="ConfirmBookingCommand"/> the REST endpoint (<c>Program.cs</c>,
/// <c>POST /api/bookings/confirm</c>) sends through <see cref="IMediator"/>,
/// so the atomic-decrement logic (ADR-0011) has exactly one implementation
/// regardless of which transport a caller used to reach it.
///
/// <see cref="Domain.Channel.Agent"/> is hardcoded here, never accepted from
/// the caller — this RPC exists specifically for Lakbay.AgentOps; an online
/// booking has no reason to reach Lakbay.Booking through it.
/// </summary>
public sealed class BookingConfirmGrpcService(IMediator mediator) : BookingConfirmService.BookingConfirmServiceBase
{
    public override async Task<ConfirmAgentBookingReply> ConfirmAgentBooking(
        ConfirmAgentBookingRequest request,
        ServerCallContext context)
    {
        if (!DateOnly.TryParse(request.DateSlot, out var dateSlot))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"date_slot '{request.DateSlot}' is not a valid ISO 8601 date."));
        }

        var command = new ConfirmBookingCommand(
            request.ProductId,
            dateSlot,
            request.CustomerId,
            Channel.Agent);

        var result = await mediator.Send(command, context.CancellationToken);

        return result.Status switch
        {
            ConfirmBookingStatus.Confirmed => new ConfirmAgentBookingReply
            {
                Status = ConfirmAgentBookingStatus.Confirmed,
                BookingId = result.BookingId!.Value.ToString(),
                PaymentStatus = result.PaymentStatus!.Value.ToString(),
                Message = result.Message
            },
            ConfirmBookingStatus.NotAvailable => new ConfirmAgentBookingReply
            {
                Status = ConfirmAgentBookingStatus.NotAvailable,
                Message = result.Message
            },
            // PaymentGatewayNotImplemented is an Online-channel outcome only
            // (ADR-0026) — Channel.Agent never routes through
            // IPaymentGateway, so ConfirmBookingCommandHandler cannot
            // actually return this status for a request this service sends.
            _ => throw new RpcException(new Status(
                StatusCode.Internal,
                $"Unexpected confirm-booking status for the Agent channel: {result.Status}."))
        };
    }
}
