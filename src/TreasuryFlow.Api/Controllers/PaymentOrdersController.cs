using MediatR;
using Microsoft.AspNetCore.Mvc;
using TreasuryFlow.Api.Contracts.PaymentOrders;
using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;
using TreasuryFlow.Application.PaymentOrders.Queries.GetPaymentOrderById;

namespace TreasuryFlow.Api.Controllers;

[ApiController]
[Route("api/payment-orders")]
public sealed class PaymentOrdersController(
    ISender sender)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetPaymentOrderByIdResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetPaymentOrderByIdQuery(
            id);

        var result = await sender.Send(
            query,
            cancellationToken);

        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Payment order not found.",
                detail:
                    $"No payment order was found with id '{id}'.");
        }

        var response = new GetPaymentOrderByIdResponse(
            result.Id,
            result.Description,
            result.Amount,
            result.Currency,
            result.Beneficiary,
            result.Status.ToString(),
            result.CreatedAt,
            result.ProcessedAt);

        return Ok(
            response);
    }

    [HttpPost]
    [ProducesResponseType<CreatePaymentOrderResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        CreatePaymentOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePaymentOrderCommand(
            request.Description,
            request.Amount,
            request.Currency,
            request.Beneficiary);

        var paymentOrderId = await sender.Send(
            command,
            cancellationToken);

        var response = new CreatePaymentOrderResponse(
            paymentOrderId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = paymentOrderId },
            response);
    }
}
