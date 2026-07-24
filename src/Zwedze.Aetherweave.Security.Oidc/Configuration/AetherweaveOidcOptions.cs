using System.ComponentModel.DataAnnotations;

namespace Zwedze.Aetherweave.Security.Oidc.Configuration;

public sealed record AetherweaveOidcOptions
{
    [Required(AllowEmptyStrings = false)] public required string Authority { get; init; }
    [Required(AllowEmptyStrings = false)] public required string ClientId { get; init; }
    [Required(AllowEmptyStrings = false)] public required string ResponseType { get; init; }
    [Required(AllowEmptyStrings = false)] public required IList<string> DefaultScopes { get; init; }
    [Required(AllowEmptyStrings = false)] public required string RedirectUri { get; init; }
    [Required(AllowEmptyStrings = false)] public required string PostLogoutRedirectUri { get; init; }
}
