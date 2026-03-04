using ChatApp.Application.ChatRooms.Commands;
using ChatApp.Application.ChatRooms.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatRoomsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllChatRoomsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChatRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateChatRoomCommand(request.Name, request.Description ?? ""), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetAll), new { id = result.Value!.Id }, result.Value);
    }
}

public record CreateChatRoomRequest(string Name, string? Description);
