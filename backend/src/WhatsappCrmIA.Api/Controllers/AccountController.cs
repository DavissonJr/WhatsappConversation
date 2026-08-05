using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Account;

namespace WhatsappCrmIA.Api.Controllers;

public record UpdateProfileRequest(string FullName, string Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    public AccountController(IMediator mediator) => _mediator = mediator;

    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMeQuery(), ct);
        return result is null ? Unauthorized() : Ok(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var (success, error) = await _mediator.Send(new UpdateProfileCommand(request.FullName, request.Email), ct);
        return success ? Ok() : BadRequest(new { message = error });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var (success, error) = await _mediator.Send(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
        return success ? Ok() : BadRequest(new { message = error });
    }
}
