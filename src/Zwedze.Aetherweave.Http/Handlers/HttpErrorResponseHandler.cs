using System.Net;

namespace Zwedze.Aetherweave.Http.Handlers;

internal sealed class HttpErrorResponseHandler(IHttpErrorHandler httpErrorHandler) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await httpErrorHandler.HandleError(request, response, response.StatusCode);
        }

        return response;
    }
}

public interface IHttpErrorHandler
{
    public Task HandleError(
        HttpRequestMessage request,
        HttpResponseMessage response,
        HttpStatusCode httpStatusCode);
}
