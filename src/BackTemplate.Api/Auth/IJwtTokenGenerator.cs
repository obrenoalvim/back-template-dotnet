using BackTemplate.Api.Data.Entities;

namespace BackTemplate.Api.Auth;

public interface IJwtTokenGenerator
{
    AuthResponse Generate(User user);
}
