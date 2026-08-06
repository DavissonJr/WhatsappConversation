using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.UseCases.Auth;

namespace WhatsappCrmIA.Api.Controllers;

public record RegisterTenantRequest(
    string CompanyName, string Segment, string FullName, string Email, string Password);

public record LoginRequest(string Email, string Password);
public record VerifyRegistrationRequest(string Email, string Code);
public record ResendCodeRequest(string Email);

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterTenantCommand(
            request.CompanyName, request.Segment, request.FullName, request.Email, request.Password), ct);

        if (!result.Success && !result.RequiresVerification)
            return BadRequest(new { message = result.ErrorMessage });

        // Sem token ainda — só confirma que o código foi enviado.
        return Ok(new { requiresVerification = true });
    }

    [HttpPost("verify-registration")]
    public async Task<IActionResult> VerifyRegistration([FromBody] VerifyRegistrationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyRegistrationCommand(request.Email, request.Code), ct);

        if (!result.Success) return BadRequest(new { message = result.ErrorMessage });
        return Ok(new { token = result.Token });
    }

    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendCodeRequest request, CancellationToken ct)
    {
        var (success, error) = await _mediator.Send(new ResendVerificationCodeCommand(request.Email), ct);
        return success ? Ok() : BadRequest(new { message = error });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);

        if (!result.Success) return Unauthorized(new { message = result.ErrorMessage });
        return Ok(new { token = result.Token });
    }
}
