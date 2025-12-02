using AnalyzerCore.Api.Middleware;
using AnalyzerCore.Application;
using AnalyzerCore.Infrastructure;
using AnalyzerCore.Infrastructure.Authentication;
using AnalyzerCore.Infrastructure.Telemetry;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

            // Add JWT Authentication
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // Add Telemetry (OpenTelemetry + Prometheus metrics)
            builder.Services.AddTelemetry(builder.Configuration);

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowConfigured", policy =>
                {
                    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:3000" };

                    policy.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("X-Correlation-ID", "Token-Expired");
                });
            });

            // Add Controllers
            builder.Services.AddControllers();

            // Add Health Checks UI (development only)
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddHealthChecksUI(options =>
                {
                    options.SetEvaluationTimeInSeconds(30);
                    options.MaximumHistoryEntriesPerEndpoint(60);
                    options.AddHealthCheckEndpoint("API", "/health");
                })
                .AddInMemoryStorage();
            }

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

                // Add JWT Authentication to Swagger
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
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
            // 1. Global exception handler (catches all unhandled exceptions)
            app.UseGlobalExceptionHandler();

            // 2. Request size limit (early rejection of oversized requests)
            app.UseRequestSizeLimit();

            // 3. Security headers (added to all responses)
            app.UseSecurityHeaders();

            // 4. Correlation ID (for request tracing)
            app.UseCorrelationId();

            // 5. Request logging with sensitive data masking
            app.UseRequestLogging();

            // 6. CORS
            app.UseCors("AllowConfigured");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Blockchain Analyzer API v1");
                    options.RoutePrefix = string.Empty; // Swagger at root
                });
            }

            // HSTS for production
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Health checks endpoints
            // Main health check - all checks
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // Liveness probe - just checks if the app is running
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // Readiness probe - checks if the app can handle requests
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // Health Checks UI (development only)
            if (app.Environment.IsDevelopment())
            {
                app.MapHealthChecksUI(options =>
                {
                    options.UIPath = "/health-ui";
                });
            }

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
