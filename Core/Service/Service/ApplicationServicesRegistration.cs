using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ServiceAbstraction;

namespace Service;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AssemblyReference).Assembly));

        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ISemesterService, SemesterService>();
        services.AddScoped<IGradingScaleService, GradingScaleService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IGpaCalculatorService, GpaCalculatorService>();
        services.AddScoped<IServiceManager, ServiceManager>();

        return services;
    }
}
