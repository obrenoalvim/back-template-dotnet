using BackTemplate.Api.Auth;
using BackTemplate.Api.Repositories;
using BackTemplate.Tests.TestFixtures;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackTemplate.Tests.Auth;

[Collection(nameof(PostgresCollection))]
public class AuthServiceTests(PostgresFixture fixture)
{
    private static IJwtTokenGenerator CreateTokenGenerator() => new JwtTokenGenerator(
        Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "unit-test-signing-key-needs-32-bytes-min",
            ExpiryMinutes = 30,
        }));

    [Fact]
    public async Task RegisterAsync_creates_user_and_returns_token()
    {
        await using var context = fixture.CreateContext();
        var userRepository = new UserRepository(context);
        var service = new AuthService(userRepository, CreateTokenGenerator());

        var email = $"{Guid.NewGuid()}@example.com";
        var response = await service.RegisterAsync(new RegisterRequest(email, "correct-horse-battery-staple"));

        Assert.False(string.IsNullOrEmpty(response.Token));
        var stored = await userRepository.GetByEmailAsync(email);
        Assert.NotNull(stored);
        Assert.NotEqual("correct-horse-battery-staple", stored!.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_throws_when_email_already_registered()
    {
        await using var context = fixture.CreateContext();
        var userRepository = new UserRepository(context);
        var service = new AuthService(userRepository, CreateTokenGenerator());

        var email = $"{Guid.NewGuid()}@example.com";
        await service.RegisterAsync(new RegisterRequest(email, "first-password-123"));

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => service.RegisterAsync(new RegisterRequest(email, "second-password-456")));
    }

    [Fact]
    public async Task LoginAsync_returns_token_for_correct_credentials()
    {
        await using var context = fixture.CreateContext();
        var userRepository = new UserRepository(context);
        var service = new AuthService(userRepository, CreateTokenGenerator());

        var email = $"{Guid.NewGuid()}@example.com";
        await service.RegisterAsync(new RegisterRequest(email, "correct-horse-battery-staple"));

        var response = await service.LoginAsync(new LoginRequest(email, "correct-horse-battery-staple"));

        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task LoginAsync_throws_for_wrong_password()
    {
        await using var context = fixture.CreateContext();
        var userRepository = new UserRepository(context);
        var service = new AuthService(userRepository, CreateTokenGenerator());

        var email = $"{Guid.NewGuid()}@example.com";
        await service.RegisterAsync(new RegisterRequest(email, "correct-horse-battery-staple"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest(email, "wrong-password")));
    }

    [Fact]
    public async Task LoginAsync_throws_for_unknown_email()
    {
        await using var context = fixture.CreateContext();
        var userRepository = new UserRepository(context);
        var service = new AuthService(userRepository, CreateTokenGenerator());

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest("nobody@example.com", "whatever")));
    }
}
