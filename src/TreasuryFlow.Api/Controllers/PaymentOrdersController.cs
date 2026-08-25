using MediatR;
using Microsoft.AspNetCore.Mvc;
using TreasuryFlow.Api.Contracts.PaymentOrders;
using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;

namespace TreasuryFlow.Api.Controllers;

[ApiController]
[Route("api/payment-orders")]
public sealed class PaymentOrdersController(
    ISender sender)
    : ControllerBase
{
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

        return Created(
            $"/api/payment-orders/{paymentOrderId}",
            response);
    }
}
