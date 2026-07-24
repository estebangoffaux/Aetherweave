using Microsoft.AspNetCore.Components;

namespace Zwedze.Aetherweave.Test.Blazor.Pages;

public partial class Authentication : ComponentBase
{
    [Parameter] public required string Action { get; set; }
}
