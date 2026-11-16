using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;

namespace StudentGPAManagementSystem.API.MiddeelWare
{
    public class GlobalErrorHandlingMiddleWare
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlingMiddleWare> _logger;

        public GlobalErrorHandlingMiddleWare(
            RequestDelegate next,
            ILogger<GlobalErrorHandlingMiddleWare> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                if (context.Response.StatusCode == StatusCodes.Status404NotFound
                    && !context.Response.HasStarted)
                {
                    await HandleNotFoundEndpointAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleErrorAsync(context, ex);
            }
        }

        private static async Task HandleErrorAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                NotFoundException or KeyNotFoundException =>
                    (HttpStatusCode.NotFound, ex.Message),
                BadRequestException or BusinessRuleException =>
                    (HttpStatusCode.BadRequest, ex.Message),
                ConflictException =>
                    (HttpStatusCode.Conflict, ex.Message),
                DbUpdateConcurrencyException =>
                    (HttpStatusCode.Conflict, "A concurrency conflict occurred. The resource was modified by another operation. Please retry."),
                UnAuthorizedException =>
                    (HttpStatusCode.Unauthorized, ex.Message),
                _ =>
                    (HttpStatusCode.InternalServerError, "An unexpected server error occurred. Please try again later.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = Response<object>.Failure(message, statusCode);

            await context.Response.WriteAsJsonAsync(response);
        }

        private static async Task HandleNotFoundEndpointAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            var response = Response<object>.Failure("Endpoint not found.", HttpStatusCode.NotFound);

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
