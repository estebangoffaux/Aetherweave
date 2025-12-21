namespace Zwedze.Aetherweave.IdentityGenerators.Exceptions;

internal sealed class InstanceIdConversionFromMachineNameErrorException() : UidGeneratorException($"The current machine name ({Environment.MachineName}) is not in the format of <NAME><NUMBER>. Creating an instance id from the hostname for the Uid Generator will only work if the hostname has the correct format.");
