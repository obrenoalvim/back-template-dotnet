using BackTemplate.Api.Data.Entities;
using BackTemplate.Api.Repositories;

namespace BackTemplate.Api.Auth;

public class AuthService(IUserRepository users, IJwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new EmailAlreadyRegisteredException(request.Email);
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        await users.AddAsync(user, cancellationToken);
        return tokenGenerator.Generate(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return tokenGenerator.Generate(user);
    }
}
