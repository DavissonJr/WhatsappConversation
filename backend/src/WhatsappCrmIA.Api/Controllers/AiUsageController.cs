using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.AiUsage;

namespace WhatsappCrmIA.Api.Controllers;

public record AddCreditsRequest(decimal AmountUsd);

[ApiController]
[Route("api/ai-usage")]
[Authorize]
public class AiUsageController : ControllerBase
{
    private readonly IMediator _mediator;
    public AiUsageController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<AiUsageSummaryDto>> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAiUsageQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // TEMPORÁRIO: em produção isso deveria estar atrás de um pagamento real
    // (Stripe/Mercado Pago), não um endpoint que qualquer usuário logado do
    // tenant pode chamar direto. Fica assim por enquanto pra você conseguir
    // testar o sistema de créditos sem precisar integrar um gateway de pagamento.
    [HttpPost("add-credits")]
    public async Task<IActionResult> AddCredits([FromBody] AddCreditsRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new AddAiCreditsCommand(request.AmountUsd), ct);
        return success ? Ok() : BadRequest(new { message = "Não foi possível adicionar créditos." });
    }
}
