// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Project_1;

public static class Program
{
    public static async Task Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddSerilogLogging()
            .BuildServiceProvider();
        await ActivatorUtilities
            .CreateInstance<MatrixMult>(serviceProvider)
            .Multiply();
    }

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        var providers = new LoggerProviderCollection();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Providers(providers)
            .CreateLogger();

        return services
            .AddSingleton(providers)
            .AddSingleton<ILoggerFactory>(serviceProvider =>
            {
                var providerCollection = serviceProvider.GetService<LoggerProviderCollection>();
                var factory = new SerilogLoggerFactory(null, true, providerCollection);

                foreach (var provider in serviceProvider.GetServices<ILoggerProvider>())
                    factory.AddProvider(provider);

                return factory;
            })
            .AddLogging()
            .AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultLogger"));
    }
}

public class MatrixMult
{
    private readonly ILogger<MatrixMult> _logger;

    public MatrixMult(ILogger<MatrixMult> logger)
    {
        _logger = logger;
    }

    public async Task Multiply()
    {
        
        await Task.WhenAll(Naive());
        _logger.LogInformation("[{ThreadId}] All done", Thread.CurrentThread.ManagedThreadId);
    }

    public async Task Naive()
    {
        _logger.LogInformation("[{ThreadId}] Hello World", Thread.CurrentThread.ManagedThreadId);
        await Task.Delay(500);
    }
    
}