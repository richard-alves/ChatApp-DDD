using ChatApp.Application.Auth.Commands;
using ChatApp.Application.Common;

namespace ChatApp.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
