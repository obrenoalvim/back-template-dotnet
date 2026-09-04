using BackTemplate.Api.Auth;
using BackTemplate.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BackTemplate.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(RateLimiterPolicies.Auth)]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (EmailAlreadyRegisteredException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidCredentialsException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
