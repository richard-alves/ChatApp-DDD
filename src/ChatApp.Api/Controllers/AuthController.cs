using ChatApp.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegisterCommand(request.Username, request.Email, request.Password), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }
}

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
