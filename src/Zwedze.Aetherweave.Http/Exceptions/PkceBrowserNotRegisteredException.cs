namespace Zwedze.Aetherweave.Http.Exceptions;

public sealed class PkceBrowserNotRegisteredException(string schemeName)
    : AetherweaveHttpException(FormatMessage(schemeName))
{
    public string SchemeName { get; } = schemeName;

    private static string FormatMessage(string schemeName)
    {
        return $"No IBrowser is registered for PKCE scheme '{schemeName}'. " +
               $"Register one with services.AddKeyedSingleton<IBrowser>(\"{schemeName}\", ...) before making requests through this HttpClient.";
    }
}
