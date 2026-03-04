using ChatApp.Application.Common;
using MediatR;

namespace ChatApp.Application.Auth.Commands;

// --- Register ---
public record RegisterCommand(string Username, string Email, string Password) : IRequest<Result<AuthResponseDto>>;

// --- Login ---
public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponseDto>>;

public record AuthResponseDto(string Token, string UserId, string Username, string Email);
