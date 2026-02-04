using System;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace VideoScheduler.Application.Tests.Helpers;

/// <summary>
/// Helper for composing test scenarios with DI-based services.
/// Allows test-specific registration of mocks and real implementations
/// without pulling in WPF types.
/// </summary>
public sealed class TestServiceProvider : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    private TestServiceProvider(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a new test service provider with the given configuration.
    /// </summary>
    public static TestServiceProvider Create(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return new TestServiceProvider(services.BuildServiceProvider());
    }

    /// <summary>
    /// Gets a service from the container.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the container, or null if not registered.
    /// </summary>
    public T? GetService<T>()
    {
        return _serviceProvider.GetService<T>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}

/// <summary>
/// Extension methods for common test setup patterns.
/// </summary>
public static class TestServiceExtensions
{
    /// <summary>
    /// Registers a substitute (mock) for the specified interface.
    /// </summary>
    public static IServiceCollection AddSubstitute<TInterface>(this IServiceCollection services)
        where TInterface : class
    {
        var substitute = Substitute.For<TInterface>();
        services.AddSingleton(substitute);
        return services;
    }

    /// <summary>
    /// Registers a substitute (mock) with configuration callback.
    /// </summary>
    public static IServiceCollection AddSubstitute<TInterface>(
        this IServiceCollection services,
        Action<TInterface> configure)
        where TInterface : class
    {
        var substitute = Substitute.For<TInterface>();
        configure(substitute);
        services.AddSingleton(substitute);
        return services;
    }
}
