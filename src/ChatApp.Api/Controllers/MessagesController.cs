using ChatApp.Application.Messages.Commands;
using ChatApp.Application.Messages.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/chatrooms/{chatRoomId}/messages")]
[Authorize]
public class MessagesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMessages(Guid chatRoomId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMessagesQuery(chatRoomId), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(Guid chatRoomId, [FromBody] PostMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value!;
        var username = User.FindFirst("username")?.Value ?? "Unknown";

        var result = await mediator.Send(
            new PostMessageCommand(chatRoomId, request.Content, userId, username),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("bot")]
    [AllowAnonymous]
    public async Task<IActionResult> PostBotMessage(Guid chatRoomId, [FromBody] PostMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PostBotMessageCommand(chatRoomId, request.Content), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}

public record PostMessageRequest(string Content);
