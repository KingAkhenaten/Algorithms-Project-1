// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Project_1;

public static class Program
{
    public static void Main()
    {
        // dependency injection stuff for serilog 
        var serviceProvider = new ServiceCollection()
            .AddSerilogLogging()
            .BuildServiceProvider();
        ActivatorUtilities
            .CreateInstance<MatrixMult>(serviceProvider)
            .Multiply();
    }

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        // set up the logger
        var providers = new LoggerProviderCollection();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss.ffff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
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

    public void Multiply()
    {
        // set up our matrices
        int[,] a = {{1, 2}, {3, 4}};
        int[,] b = {{2, 2}, {2, 2}};
        int[,] c = new int[2,2];
        var n = 2;
        
        Naive(a, b, c, 2);
        _logger.LogInformation("All done");
    }
    private void Naive(int [,] a, int[,] b, int[,] c, int n)
    {
        var count = 0;
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        _logger.LogInformation("Start Naive Approach");
        // loop through the matrix rows and columns
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                for (int k = 0; k < n; k++)
                {
                    count += 1;
                    c[i, j] = c[i, j] + a[i, k] * b[k, j];
                }
            }
        }

        _logger.LogInformation("Ended with {count} operations and {time} ms elapsed time",
            count, watch.ElapsedMilliseconds);
    }
    
}