using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ServeFlow.UnitTests;

public class AuthApiIntegrationTests : IAsyncLifetime
{
    private ProductsApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ProductsApiFactory();
        _client = _factory.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_AssignsStaffRole_EvenWhenAdminRoleIsRequested()
    {
        var email = $"registration-{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                fullName = "Registration Test User",
                email,
                password = "SecureTest123!",
                role = "Admin"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        var user = result.GetProperty("user");

        Assert.Equal(
            "Staff",
            user.GetProperty("role").GetString());

        Assert.Equal(
            email,
            user.GetProperty("email").GetString());
    }
}
