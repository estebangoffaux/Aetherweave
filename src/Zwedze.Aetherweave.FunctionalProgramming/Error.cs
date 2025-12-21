using JetBrains.Annotations;

namespace Zwedze.Aetherweave.FunctionalProgramming;

[UsedImplicitly]
public sealed record Error(string Code, string Message);
