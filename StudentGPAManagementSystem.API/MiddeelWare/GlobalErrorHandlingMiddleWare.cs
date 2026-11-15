using System;
using System.Threading.Tasks;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.ErrorModels;

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

                // Handle 404 endpoints
                if (context.Response.StatusCode == StatusCodes.Status404NotFound
                    && !context.Response.HasStarted)
                {
                    await HandleNotFoundEndpointAsync(context);
                }
            }
            catch (Exception ex)
            {
                // Log full error internally
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);

                await HandleErrorAsync(context, ex);
            }
        }

        private static async Task HandleErrorAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var statusCode = ex switch
            {
                NotFoundExceptions => StatusCodes.Status404NotFound,
                BadRequestException => StatusCodes.Status400BadRequest,
                UnAuthorizedException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError,
            };

            context.Response.StatusCode = statusCode;

            var response = new ErrorDetails
            {
                StatusCode = statusCode,
                ErrorMessage = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        private static async Task HandleNotFoundEndpointAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            var response = new ErrorDetails
            {
                StatusCode = 404,
                ErrorMessage = "Endpoint not found."
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
