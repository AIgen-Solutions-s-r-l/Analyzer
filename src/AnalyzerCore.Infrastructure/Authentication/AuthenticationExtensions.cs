using System.Text;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// Extension methods for authentication registration.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT and API Key authentication services.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure JWT settings
        services
            .AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        // Register authentication services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IApiKeyGenerator, ApiKeyGenerator>();

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        // Configure authentication with multiple schemes
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "MultiAuth";
            options.DefaultChallengeScheme = "MultiAuth";
            options.DefaultScheme = "MultiAuth";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                // Handle WebSocket/SignalR authentication via query string
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    // If the request is for a hub and has a token in query string
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationDefaults.AuthenticationScheme, options => { })
        .AddPolicyScheme("MultiAuth", "JWT or API Key", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // Check for API Key first (header or query)
                if (context.Request.Headers.ContainsKey("X-API-Key") ||
                    context.Request.Query.ContainsKey("api_key"))
                {
                    return ApiKeyAuthenticationDefaults.AuthenticationScheme;
                }

                // For SignalR connections, check for access_token in query string
                var path = context.Request.Path;
                if (path.StartsWithSegments("/hubs") &&
                    context.Request.Query.ContainsKey("access_token"))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }

                // Default to JWT Bearer
                return JwtBearerDefaults.AuthenticationScheme;
            };
        });

        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireUser", policy =>
                policy.RequireRole("Admin", "User"));

            options.AddPolicy("RequireReadOnly", policy =>
                policy.RequireRole("Admin", "User", "ReadOnly"));
        });

        return services;
    }
}
