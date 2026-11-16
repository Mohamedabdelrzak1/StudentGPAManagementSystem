using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Common;
using StudentGPAManagementSystem.API.MiddeelWare;
using Xunit;

namespace StudentGPAManagementSystem.Tests.ErrorHandling;

public class GlobalErrorHandlingMiddleWareTests
{
    private static async Task<(int statusCode, Response<object> envelope, string rawJson)> RunMiddlewareAsync(RequestDelegate next)
    {
        var middleware = new GlobalErrorHandlingMiddleWare(next, NullLogger<GlobalErrorHandlingMiddleWare>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var rawJson = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = JsonSerializer.Deserialize<Response<object>>(rawJson, options)!;

        return (context.Response.StatusCode, envelope, rawJson);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404WithUnifiedEnvelope()
    {
        var (statusCode, envelope, _) = await RunMiddlewareAsync(_ => throw new NotFoundException("Item with ID 5 was not found."));

        Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        Assert.Equal(HttpStatusCode.NotFound, envelope.StatusCode);
        Assert.False(envelope.Succeeded);
        Assert.Equal("Item with ID 5 was not found.", envelope.Message);
        Assert.NotNull(envelope.Errors);
        Assert.Contains("Item with ID 5 was not found.", envelope.Errors);
        Assert.Null(envelope.ValidationErrors);
        Assert.Null(envelope.Payload);
    }

    [Fact]
    public async Task InvokeAsync_BusinessRuleException_Returns400WithUnifiedEnvelope()
    {
        var (statusCode, envelope, _) = await RunMiddlewareAsync(_ => throw new BusinessRuleException("18-hour semester limit exceeded."));

        Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        Assert.Equal(HttpStatusCode.BadRequest, envelope.StatusCode);
        Assert.False(envelope.Succeeded);
        Assert.Equal("18-hour semester limit exceeded.", envelope.Message);
        Assert.NotNull(envelope.Errors);
        Assert.Contains("18-hour semester limit exceeded.", envelope.Errors);
        Assert.Null(envelope.ValidationErrors);
        Assert.Null(envelope.Payload);
    }

    [Fact]
    public async Task InvokeAsync_BadRequestException_Returns400WithUnifiedEnvelope()
    {
        var (statusCode, envelope, _) = await RunMiddlewareAsync(_ => throw new BadRequestException("Invalid request input."));

        Assert.Equal((int)HttpStatusCode.BadRequest, statusCode);
        Assert.Equal(HttpStatusCode.BadRequest, envelope.StatusCode);
        Assert.False(envelope.Succeeded);
        Assert.Equal("Invalid request input.", envelope.Message);
        Assert.NotNull(envelope.Errors);
        Assert.Contains("Invalid request input.", envelope.Errors);
        Assert.Null(envelope.ValidationErrors);
        Assert.Null(envelope.Payload);
    }

    [Fact]
    public async Task InvokeAsync_ConflictException_Returns409WithUnifiedEnvelope()
    {
        var (statusCode, envelope, _) = await RunMiddlewareAsync(_ => throw new ConflictException("Course code CS101 already exists."));

        Assert.Equal((int)HttpStatusCode.Conflict, statusCode);
        Assert.Equal(HttpStatusCode.Conflict, envelope.StatusCode);
        Assert.False(envelope.Succeeded);
        Assert.Equal("Course code CS101 already exists.", envelope.Message);
        Assert.NotNull(envelope.Errors);
        Assert.Contains("Course code CS101 already exists.", envelope.Errors);
        Assert.Null(envelope.ValidationErrors);
        Assert.Null(envelope.Payload);
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateConcurrencyException_Returns409WithUnifiedEnvelope()
    {
        var (statusCode, envelope, _) = await RunMiddlewareAsync(_ => throw new DbUpdateConcurrencyException("Concurrency check failed."));

        Assert.Equal((int)HttpStatusCode.Conflict, statusCode);
        Assert.Equal(HttpStatusCode.Conflict, envelope.StatusCode);
        Assert.False(envelope.Succeeded);
        Assert.Contains("concurrency conflict", envelope.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(envelope.Errors);
        Assert.Null(envelope.ValidationErrors);
        Assert.Null(envelope.Payload);
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_Returns500WithoutLeakingMessageOrStackTrace()
    {
        const string secretMessage = "SQL server password failed at internal db connection string";
        var (statusCode, envelope, rawJson) = await RunMiddlewareAsync(_ => throw new InvalidOperationException(secretMessage));

        Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, envelope.StatusCode);
        Assert.False(envelope.Succeeded);
        Assert.Equal("An unexpected server error occurred. Please try again later.", envelope.Message);
        Assert.NotNull(envelope.Errors);
        Assert.Contains("An unexpected server error occurred. Please try again later.", envelope.Errors);
        Assert.Null(envelope.ValidationErrors);
        Assert.Null(envelope.Payload);

        // Crucial safety check: ensure secret message and stack trace are NEVER leaked
        Assert.DoesNotContain(secretMessage, rawJson);
        Assert.DoesNotContain("StackTrace", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at StudentGPAManagementSystem", rawJson, StringComparison.OrdinalIgnoreCase);
    }
}
