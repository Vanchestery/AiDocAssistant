using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace AiDocAssistant.Tests;

/// <summary>
/// Smoke-тесты: приложение поднимается и отвечает.
/// Окружение "Testing" отключает автомиграцию, поэтому БД не нужна.
/// </summary>
public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b => b.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task Swagger_endpoint_responds_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.True(response.IsSuccessStatusCode,
            $"Swagger вернул {(int)response.StatusCode}");
    }
}
