using Zwedze.Aetherweave.Http.Exceptions;

namespace Zwedze.Aetherweave.Security.Oidc.Native.Exceptions;

public sealed class AetherweaveAuthenticationException(string schemeName, string error)
    : AetherweaveHttpException(FormatMessage(schemeName, error))
{
    public string SchemeName { get; } = schemeName;

    public string Error { get; } = error;

    private static string FormatMessage(string schemeName, string error)
    {
        return $"Authentication failed for scheme '{schemeName}': {error}";
    }
}
