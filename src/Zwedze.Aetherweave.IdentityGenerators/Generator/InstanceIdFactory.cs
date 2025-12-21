using System.Net;
using System.Text.RegularExpressions;
using Zwedze.Aetherweave.IdentityGenerators.Exceptions;

namespace Zwedze.Aetherweave.IdentityGenerators.Generator;

/// <summary>
///     Factory that creates instance ids based on environment properties.
/// </summary>
internal interface IInstanceIdFactory
{
    /// <summary>
    ///     When the host name is matching the typical format of SERVERNAME + NUMBER, it will extract the number from it.
    ///     For LNXSRV03, it will return 3
    /// </summary>
    /// <param name="hostName"></param>
    /// <returns>The number suffix of the hostname</returns>
    int TransformHostnameToInstanceId(string hostName);

    /// <summary>
    ///     Creates an instance id from an ip address. He will use the 2 most specific bytes and create an int from it.
    ///     192.168.1.1 will become 0000000100000001 so id 257
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    /// <exception cref="InstanceIdConversionFromMachineNameErrorException"></exception>
    int TransformIpToInstanceId(IPAddress address);
}

/// <inheritdoc />
internal sealed class InstanceIdFactory : IInstanceIdFactory
{
    private static readonly Regex Regex = new(@"^.+?(?<number>\d+)$");

    /// <inheritdoc />
    public int TransformHostnameToInstanceId(string hostName)
    {
        var match = Regex.Match(hostName);
        if (!match.Success)
        {
            throw new InstanceIdConversionFromMachineNameErrorException();
        }

        return int.Parse(match.Groups["number"].Value);
    }

    /// <inheritdoc />
    public int TransformIpToInstanceId(IPAddress address)
    {
        var ipBytes = address.GetAddressBytes();
        if (ipBytes.Length != 4)
        {
            throw new ArgumentException("The given IP address is not of the correct format.");
        }

        ipBytes[0] = 0;
        ipBytes[1] = 0;

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ipBytes);
        }

        return BitConverter.ToInt32(ipBytes);
    }
}
