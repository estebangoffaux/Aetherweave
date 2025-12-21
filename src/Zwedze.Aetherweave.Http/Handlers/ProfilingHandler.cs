using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Zwedze.Aetherweave.Http.Handlers;

internal sealed class ProfilingHandler(
    ILogger<ProfilingHandler> logger,
    bool isProfilingEnabled) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!isProfilingEnabled)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "HTTP {Method} {RequestUri} completed in {ElapsedMs}ms with status {StatusCode}",
            request.Method,
            request.RequestUri,
            stopwatch.ElapsedMilliseconds,
            (int)response.StatusCode);

        return response;
    }
}
