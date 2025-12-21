namespace Zwedze.Aetherweave.IdentityGenerators.Exceptions;

internal sealed class UidGeneratorNoIpException() : UidGeneratorException("No network adapters with an IPv4 address in the system!");
