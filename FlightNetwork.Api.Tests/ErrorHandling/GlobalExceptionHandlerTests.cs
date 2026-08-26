using System.Text.Json;
using FlightNetwork.Api.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlightNetwork.Api.Tests.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _sut = new(NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_ArgumentException_Returns400_WithExceptionMessageAsDetail()
    {
        var context = CreateHttpContext();
        var exception = new ArgumentOutOfRangeException("page", "page must be at least 1.");

        var handled = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var problem = await ReadProblemDetailsAsync(context);
        Assert.Equal(400, problem.Status);
        Assert.Contains("page must be at least 1", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_Returns500_WithoutLeakingExceptionMessage()
    {
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Neo4j connection string contains a secret");

        var handled = await _sut.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var problem = await ReadProblemDetailsAsync(context);
        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("secret", problem.Detail ?? string.Empty);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/airports";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<ProblemDetails> ReadProblemDetailsAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
