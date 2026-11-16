using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence;
using Persistence.Data;
using Service;
using Shared.ErrorModels;
using StudentGPAManagementSystem.API.MiddeelWare;

namespace StudentGPAManagementSystem.API.Extensions;

public static class Extensions
{
    public static IServiceCollection RegisterAllServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerService();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices();

        services.Configure<ApiBehaviorOptions>(config =>
        {
            config.InvalidModelStateResponseFactory = actionContext =>
            {
                var validationErrors = actionContext.ModelState
                    .Where(m => m.Value?.Errors.Any() == true)
                    .Select(m => new Shared.Common.ValidationErrorDetail
                    {
                        Field = m.Key,
                        Errors = m.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                    })
                    .ToList();

                var response = Shared.Common.Response<object>.ValidationFailure("One or more validation errors occurred.", validationErrors);

                return new BadRequestObjectResult(response);
            };
        });

        return services;
    }

    private static IServiceCollection AddSwaggerService(this IServiceCollection services)
    {
        services.AddSwaggerGen();

        return services;
    }

    public static async Task<WebApplication> ConfigurMiddelwares(this WebApplication app)
    {
        await app.InitialiseDatabaseAsync();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student GPA Management System API v1");
            c.RoutePrefix = "swagger";
        });

        app.UseMiddleware<GlobalErrorHandlingMiddleWare>();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/" || context.Request.Path == "")
            {
                context.Response.Redirect("/swagger");
                return;
            }
            await next();
        });

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowAll");

        app.MapControllers();

        return app;
    }

    public static async Task<WebApplication> InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<StudentGpaDbContext>();
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");

            await DbInitializer.SeedSampleDataAsync(dbContext);
            logger.LogInformation("Sample data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Critical error during database initialization.");
        }

        return app;
    }
}
