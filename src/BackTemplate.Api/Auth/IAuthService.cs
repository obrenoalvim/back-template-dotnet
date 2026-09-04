namespace BackTemplate.Api.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public class EmailAlreadyRegisteredException(string email)
    : Exception($"Email already registered: {email}");

public class InvalidCredentialsException()
    : Exception("Invalid email or password");
