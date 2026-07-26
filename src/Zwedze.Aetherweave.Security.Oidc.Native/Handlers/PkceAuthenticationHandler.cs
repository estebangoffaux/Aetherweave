using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Security.Oidc.Native.Configuration;
using Zwedze.Aetherweave.Security.Oidc.Native.Exceptions;

namespace Zwedze.Aetherweave.Security.Oidc.Native.Handlers;

internal sealed class PkceAuthenticationHandler(
    IOptionsMonitor<PkceSchemeOptions> optionsMonitor,
    IServiceProvider serviceProvider,
    string schemeName) : DelegatingHandler
{
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private DelegatingHandler? _refreshTokenHandler;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var refreshTokenHandler = await GetRefreshTokenHandlerAsync(cancellationToken);

        using var invoker = new HttpMessageInvoker(refreshTokenHandler, disposeHandler: false);
        return await invoker.SendAsync(request, cancellationToken);
    }

    private async Task<DelegatingHandler> GetRefreshTokenHandlerAsync(CancellationToken cancellationToken)
    {
        if (_refreshTokenHandler is not null)
        {
            return _refreshTokenHandler;
        }

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (_refreshTokenHandler is not null)
            {
                return _refreshTokenHandler;
            }

            var browser = serviceProvider.GetKeyedService<IBrowser>(schemeName)
                ?? throw new PkceBrowserNotRegisteredException(schemeName);

            var options = optionsMonitor.Get(schemeName);
            var oidcClient = new OidcClient(new OidcClientOptions
            {
                Authority = options.Authority,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                Scope = options.Scope,
                RedirectUri = options.RedirectUri,
                Browser = browser,
            });

            var result = await oidcClient.LoginAsync(new LoginRequest(), cancellationToken);
            if (result.IsError)
            {
                throw new AetherweaveAuthenticationException(schemeName, result.Error);
            }

            if (result.RefreshTokenHandler is null)
            {
                throw new AetherweaveAuthenticationException(
                    schemeName,
                    "Login succeeded but no refresh token was issued. Ensure the 'offline_access' scope is requested.");
            }

            result.RefreshTokenHandler.InnerHandler = InnerHandler!;
            _refreshTokenHandler = result.RefreshTokenHandler;
            return _refreshTokenHandler;
        }
        finally
        {
            _loginLock.Release();
        }
    }
}
