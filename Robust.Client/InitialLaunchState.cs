using System.Net;

namespace Robust.Client
{
    public sealed class InitialLaunchState
    {
        public bool FromLauncher { get; }
        public string? ConnectAddress { get; }
        public string? Ss14Address { get; }
        public DnsEndPoint? ConnectEndpoint { get; }

        public InitialLaunchState(bool fromLauncher, string? connectAddress, string? ss14Address, DnsEndPoint? connectEndpoint)
        {
            FromLauncher = fromLauncher;
            ConnectAddress = connectAddress;
            Ss14Address = ss14Address;
            ConnectEndpoint = connectEndpoint;
        }
    }
}
