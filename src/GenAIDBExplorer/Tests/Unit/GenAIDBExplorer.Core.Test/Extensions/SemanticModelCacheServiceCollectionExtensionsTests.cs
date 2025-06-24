using FluentAssertions;
using GenAIDBExplorer.Core.Extensions;
using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace GenAIDBExplorer.Core.Tests.Extensions;

[TestClass]
public class SemanticModelCacheServiceCollectionExtensionsTests
{
    private IServiceCollection _services = null!;
    private Mock<ISchemaRepository> _mockInnerRepository = null!;

    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection();
        _mockInnerRepository = new Mock<ISchemaRepository>();
        
        // Add required dependencies
        _services.AddLogging();
        _services.AddSingleton(_mockInnerRepository.Object);
        _services.AddSingleton<ISchemaRepository>(_mockInnerRepository.Object);
    }

    [TestMethod]
    public void AddSemanticModelCaching_ShouldRegisterRequiredServices()
    {
        // Act
        _services.AddSemanticModelCaching();
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();
        serviceProvider.GetService<ISemanticModelCache>().Should().NotBeNull();
        serviceProvider.GetService<ISemanticModelCache>().Should().BeOfType<InMemorySemanticModelCache>();
    }

    [TestMethod]
    public void AddSemanticModelCaching_ShouldDecorateSchemaRepository()
    {
        // Act
        _services.AddSemanticModelCaching();
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var schemaRepository = serviceProvider.GetService<ISchemaRepository>();
        schemaRepository.Should().NotBeNull();
        schemaRepository.Should().BeOfType<CachedSchemaRepository>();
    }

    [TestMethod]
    public void AddSemanticModelCaching_WithOptions_ShouldConfigureMemoryCache()
    {
        // Arrange
        var sizeLimit = 500;

        // Act
        _services.AddSemanticModelCaching(options =>
        {
            options.SizeLimit = sizeLimit;
        });
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var memoryCache = serviceProvider.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull();
    }

    [TestMethod]
    public void AddSemanticModelCaching_ShouldAllowMultipleCalls()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
        {
            _services.AddSemanticModelCaching();
            _services.AddSemanticModelCaching();
        }).Should().NotThrow();
    }
}

[TestClass]
public class ServiceCollectionDecoratorExtensionsTests
{
    private IServiceCollection _services = null!;

    [TestInitialize]
    public void Setup()
    {
        _services = new ServiceCollection();
    }

    [TestMethod]
    public void Decorate_ShouldThrowException_WhenServiceNotRegistered()
    {
        // Act & Assert
        FluentActions.Invoking(() => _services.Decorate<ITestService, TestServiceDecorator>())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*ITestService*not registered*");
    }

    [TestMethod]
    public void Decorate_ShouldWrapService_WhenServiceRegistered()
    {
        // Arrange
        _services.AddSingleton<ITestService, TestService>();

        // Act
        _services.Decorate<ITestService, TestServiceDecorator>();
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ITestService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<TestServiceDecorator>();
    }

    [TestMethod]
    public void Decorate_ShouldPreserveServiceLifetime()
    {
        // Arrange
        _services.AddScoped<ITestService, TestService>();

        // Act
        _services.Decorate<ITestService, TestServiceDecorator>();

        // Assert
        var serviceDescriptor = _services.First(s => s.ServiceType == typeof(ITestService));
        serviceDescriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }
}

// Test interfaces and implementations for decorator tests
public interface ITestService
{
    string GetValue();
}

public class TestService : ITestService
{
    public string GetValue() => "Original";
}

public class TestServiceDecorator : ITestService
{
    private readonly ITestService _inner;

    public TestServiceDecorator(ITestService inner)
    {
        _inner = inner;
    }

    public string GetValue() => $"Decorated({_inner.GetValue()})";
}