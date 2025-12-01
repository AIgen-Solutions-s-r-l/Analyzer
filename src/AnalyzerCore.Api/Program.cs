using AnalyzerCore.Application;
using AnalyzerCore.Infrastructure;
using Serilog;
using Serilog.Events;

namespace AnalyzerCore.Api;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Blockchain Analyzer...");

            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                // Add Application layer (MediatR, FluentValidation, Behaviors)
                services.AddApplication();

                // Add Infrastructure layer (EF Core, Repositories, Blockchain, Background Services)
                services.AddInfrastructure(configuration);
            });
}
