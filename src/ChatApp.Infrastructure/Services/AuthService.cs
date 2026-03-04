using ChatApp.Application.Auth.Commands;
using ChatApp.Application.Common;
using ChatApp.Application.Interfaces;
using ChatApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatApp.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) : IAuthService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return Result<AuthResponseDto>.Failure("Email already registered.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = username
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

        var token = GenerateJwtToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(token, user.Id, username, email));
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<AuthResponseDto>.Failure("Invalid credentials.");

        var isValid = await userManager.CheckPasswordAsync(user, password);
        if (!isValid)
            return Result<AuthResponseDto>.Failure("Invalid credentials.");

        var token = GenerateJwtToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(token, user.Id, user.DisplayName, email));
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("username", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
