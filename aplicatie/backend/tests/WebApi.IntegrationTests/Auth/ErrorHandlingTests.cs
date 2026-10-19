using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NutriScan.Application.Common.Exceptions;

namespace NutriScan.WebApi.IntegrationTests.Auth;

[ApiController]
[Route("test-errors")]
public class ThrowingTestController : ControllerBase
{
    [HttpGet("{kind}")]
    public IActionResult Throw(string kind) => kind switch
    {
        "validation" => throw new ValidationException(new[] { new ValidationFailure("Email", "Email invalid") }),
        "conflict" => throw new ConflictException("x"),
        "unauthorized" => throw new UnauthorizedException("y"),
        "forbidden" => throw new ForbiddenException("z"),
        "notfound" => throw new NotFoundException("w"),
        _ => throw new InvalidOperationException("secret")
    };
}

[Collection(IntegrationTestCollection.Name)]
public class ErrorHandlingTests(NutriScanApiFactory factory)
{
    private HttpClient CreateClient() =>
        factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
                s.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly)))
            .CreateClient();

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    [Fact]
    public async Task ValidationException_Returns400WithFieldErrors()
    {
        // Act
        var response = await CreateClient().GetAsync("/test-errors/validation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadProblemAsync(response);
        problem.GetProperty("title").GetString().Should().Be("Date invalide");
        problem.GetProperty("errors").GetProperty("Email")[0].GetString().Should().Be("Email invalid");
    }

    [Theory]
    [InlineData("conflict", HttpStatusCode.Conflict, "x")]
    [InlineData("unauthorized", HttpStatusCode.Unauthorized, "y")]
    [InlineData("forbidden", HttpStatusCode.Forbidden, "z")]
    [InlineData("notfound", HttpStatusCode.NotFound, "w")]
    public async Task ApplicationException_MapsToStatusAndTitle(string kind, HttpStatusCode status, string title)
    {
        // Act
        var response = await CreateClient().GetAsync($"/test-errors/{kind}");

        // Assert
        response.StatusCode.Should().Be(status);
        (await ReadProblemAsync(response)).GetProperty("title").GetString().Should().Be(title);
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutDetails()
    {
        // Act
        var response = await CreateClient().GetAsync("/test-errors/other");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("secret");
        (await ReadProblemAsync(response)).GetProperty("title").GetString().Should().Be("A apărut o eroare neașteptată");
    }
}
