using MediatR;
using Microsoft.AspNetCore.Mvc;
using TreasuryFlow.Api.Contracts.PaymentOrders;
using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;
using TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;
using TreasuryFlow.Application.PaymentOrders.Commands.UpdatePaymentOrder;
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdatePaymentOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePaymentOrderCommand(
            id,
            request.Description,
            request.Amount,
            request.Currency,
            request.Beneficiary);

        await sender.Send(
            command,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Submit(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SubmitPaymentOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/start-processing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartProcessing(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new StartProcessingPaymentOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Complete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new CompletePaymentOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Fail(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new FailPaymentOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new CancelPaymentOrderCommand(id),
            cancellationToken);

        return NoContent();
    }
}
