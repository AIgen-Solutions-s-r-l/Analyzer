using Microsoft.Extensions.Logging;
using Polly;

namespace AnalyzerCore.Infrastructure.Resilience;

/// <summary>
/// HTTP client handler with built-in resilience policies.
/// </summary>
public sealed class ResilientHttpClientHandler : DelegatingHandler
{
    private readonly IResiliencePolicyFactory _policyFactory;
    private readonly ILogger<ResilientHttpClientHandler> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public ResilientHttpClientHandler(
        IResiliencePolicyFactory policyFactory,
        ILogger<ResilientHttpClientHandler> logger)
    {
        _policyFactory = policyFactory;
        _logger = logger;
        _policy = CreatePolicy();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async ct =>
        {
            var response = await base.SendAsync(request, ct);

            // Treat certain status codes as transient failures
            if (ShouldRetryStatusCode(response.StatusCode))
            {
                _logger.LogWarning(
                    "Received transient HTTP status {StatusCode} from {Url}",
                    (int)response.StatusCode,
                    request.RequestUri);

                throw new HttpRequestException(
                    $"Transient HTTP error: {(int)response.StatusCode} {response.StatusCode}");
            }

            return response;
        }, cancellationToken);
    }

    private IAsyncPolicy<HttpResponseMessage> CreatePolicy()
    {
        return _policyFactory.CreateCombinedPolicy<HttpResponseMessage>();
    }

    private static bool ShouldRetryStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.RequestTimeout => true,
            System.Net.HttpStatusCode.TooManyRequests => true,
            System.Net.HttpStatusCode.InternalServerError => true,
            System.Net.HttpStatusCode.BadGateway => true,
            System.Net.HttpStatusCode.ServiceUnavailable => true,
            System.Net.HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }
}
