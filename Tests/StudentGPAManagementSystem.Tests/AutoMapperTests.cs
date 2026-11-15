using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Service;
using Xunit;

namespace StudentGPAManagementSystem.Tests;

public class AutoMapperTests
{
    [Fact]
    public void AutoMapperConfiguration_IsValid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        // Act
        var mapper = provider.GetRequiredService<IMapper>();

        // Assert
        Assert.NotNull(mapper);
    }
}
