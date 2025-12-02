using AnalyzerCore.Api.Middleware;
using AnalyzerCore.Application;
using AnalyzerCore.Infrastructure;
using AnalyzerCore.Infrastructure.Telemetry;
using Microsoft.OpenApi.Models;
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
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Blockchain Analyzer API...");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog();

            // Add Application layer (MediatR, FluentValidation, Behaviors)
            builder.Services.AddApplication();

            // Add Infrastructure layer (EF Core, Repositories, Blockchain, Background Services)
            builder.Services.AddInfrastructure(builder.Configuration);

            // Add Telemetry (OpenTelemetry + Prometheus metrics)
            builder.Services.AddTelemetry(builder.Configuration);

            // Add Controllers
            builder.Services.AddControllers();

            // Add Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Blockchain Analyzer API",
                    Version = "v1",
                    Description = "API for blockchain analysis - pool and token tracking",
                    Contact = new OpenApiContact
                    {
                        Name = "AIgen Solutions",
                        Email = "info@aigen.solutions"
                    }
                });

                // Include XML comments for better documentation
                var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            var app = builder.Build();

            // Configure HTTP request pipeline
            // Add correlation ID middleware first to ensure all requests have correlation ID
            app.UseCorrelationId();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Blockchain Analyzer API v1");
                    options.RoutePrefix = string.Empty; // Swagger at root
                });
            }

            // Health checks endpoint
            app.MapHealthChecks("/health");

            // Prometheus metrics endpoint
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            // Map controllers
            app.MapControllers();

            app.Run();
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
}
