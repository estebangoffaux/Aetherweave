using System.Text;
using Microsoft.Extensions.Logging;

namespace Zwedze.Aetherweave.Http.Handlers;

internal sealed class ContentTracingHandler(
    ILogger<ContentTracingHandler> logger,
    bool enableContentTracing,
    int maxContentLogSize) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!enableContentTracing)
        {
            return response;
        }

        try
        {
            // Save original headers before consuming content
            var originalHeaders = response.Content.Headers.ToDictionary(h => h.Key, h => h.Value);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var logContent = content.Length > maxContentLogSize
                ? $"{content[..maxContentLogSize]}... (truncated, total: {content.Length} bytes)"
                : content;

            logger.LogInformation(
                "HTTP {Method} {RequestUri} returned {StatusCode} ({ContentLength} bytes): {Content}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                content.Length,
                logContent);

            // Recreate content
            var encoding = GetEncodingFromContentType(response.Content.Headers.ContentType?.CharSet);
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";

            response.Content = new StringContent(content, encoding, mediaType);

            // Restore original headers
            foreach (var (key, values) in originalHeaders)
            {
                response.Content.Headers.TryAddWithoutValidation(key, values);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to trace HTTP response content for {RequestUri}", request.RequestUri);
        }

        return response;
    }

    private static Encoding GetEncodingFromContentType(string? charset)
    {
        if (string.IsNullOrEmpty(charset))
        {
            return Encoding.UTF8;
        }

        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }
}
