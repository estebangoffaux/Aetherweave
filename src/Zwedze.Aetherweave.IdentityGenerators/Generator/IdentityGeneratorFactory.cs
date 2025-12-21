using IdGen;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.IdentityGenerators.Configuration;

namespace Zwedze.Aetherweave.IdentityGenerators.Generator;

internal sealed class IdentityGeneratorFactory
{
    private readonly IEnvironmentInfoRetriever _environmentInfoRetriever;
    private readonly IInstanceIdFactory _instanceIdFactory;
    private readonly ILogger<IdentityGeneratorFactory> _logger;
    private readonly ITimeSourceFactory _timeSourceFactory;

    internal IdentityGeneratorFactory(
        IEnvironmentInfoRetriever environmentInfoRetriever,
        IInstanceIdFactory instanceIdFactory,
        ITimeSourceFactory timeSourceFactory,
        ILogger<IdentityGeneratorFactory> logger)
    {
        _environmentInfoRetriever = environmentInfoRetriever ?? throw new ArgumentNullException(nameof(environmentInfoRetriever));
        _instanceIdFactory = instanceIdFactory ?? throw new ArgumentNullException(nameof(instanceIdFactory));
        _timeSourceFactory = timeSourceFactory ?? throw new ArgumentNullException(nameof(timeSourceFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IdentityGenerator Create(UidGenerationOptions options)
    {
        return options.InstanceIdType switch
        {
            InstanceIdType.MachineName => CreateMachineNameUidGenerator(options),
            InstanceIdType.Ip => CreateIpUidGenerator(options),
            _ => throw new ArgumentException($"Invalid {nameof(options.InstanceIdType)} in the {nameof(UidGenerationOptions)}: {options.InstanceIdType}")
        };
    }

    private IdentityGenerator CreateIpUidGenerator(UidGenerationOptions options)
    {
        var ipAddress = _environmentInfoRetriever.GetLocalIpAddress();
        var instanceId = _instanceIdFactory.TransformIpToInstanceId(ipAddress);

        _logger.LogInformation("Creating UidGenerator with derived id of ip: {IpAddress}", ipAddress);
        return CreateCommonUidGenerator(
            instanceId,
            new IdStructure(41, 16, 6),
            options);
    }

    private IdentityGenerator CreateMachineNameUidGenerator(UidGenerationOptions options)
    {
        var machineName = _environmentInfoRetriever.GetMachineName();
        var instanceId = _instanceIdFactory.TransformHostnameToInstanceId(machineName);

        _logger.LogInformation(
            "Creating UidGenerator with derived id of machine name: {MachineName}",
            machineName);
        return CreateCommonUidGenerator(
            instanceId,
            new IdStructure(41, 10, 12),
            options);
    }

    private IdentityGenerator CreateCommonUidGenerator(
        int instanceId,
        IdStructure structure,
        UidGenerationOptions options)
    {
        var idGeneratorOptions = new IdGeneratorOptions(
            structure,
            _timeSourceFactory.Create(options.EpochStart),
            SequenceOverflowStrategy.SpinWait);
        var idGenerator = new IdGenerator(
            instanceId,
            idGeneratorOptions);
        return new IdentityGenerator(idGenerator);
    }
}
