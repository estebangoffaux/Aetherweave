using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Zwedze.Aetherweave.IdentityGenerators.Exceptions;

namespace Zwedze.Aetherweave.IdentityGenerators.Generator;

/// <summary>
///     Retrieves information about the environment on which the application runs.
/// </summary>
internal interface IEnvironmentInfoRetriever
{
    /// <summary>
    ///     Retrieves the machine name of the server.
    /// </summary>
    /// <returns></returns>
    string GetMachineName();

    /// <summary>
    ///     Returns the local IP address of the server.
    ///     If multiple local IP addresses are detected the first is returned.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UidGeneratorNoIpException">When no ip address can be found.</exception>
    IPAddress GetLocalIpAddress();
}

/// <inheritdoc />
internal sealed class EnvironmentInfoRetriever : IEnvironmentInfoRetriever
{
    private readonly ILogger<EnvironmentInfoRetriever> _logger;

    /// <summary>
    ///     Returns an instance of the <see cref="EnvironmentInfoRetriever" />
    /// </summary>
    /// <param name="logger">The logger that will be used for log messages.</param>
    public EnvironmentInfoRetriever(ILogger<EnvironmentInfoRetriever> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(
            nameof(logger),
            "Logger should not be empty");
    }

    /// <inheritdoc />
    public string GetMachineName()
    {
        return Environment.MachineName;
    }

    /// <inheritdoc />
    public IPAddress GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        _logger.LogDebug(
            "Searching local ip for host: {@Host}",
            host);
        var ips = host.AddressList.Where(address => address.AddressFamily == AddressFamily.InterNetwork).ToArray();
        if (ips.Length < 1)
        {
            throw new UidGeneratorNoIpException();
        }

        if (ips.Length == 1)
        {
            _logger.LogDebug(
                "Found single local ip {Ip}",
                ips[0]);
        }
        else
        {
            _logger.LogWarning(
                "Found multiple local ip's, using first one: {Ip}",
                ips[0]);
        }

        return ips[0];
    }
}
