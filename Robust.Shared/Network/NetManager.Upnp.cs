using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;
using Robust.Shared.Log;

namespace Robust.Shared.Network;

public partial class NetManager
{
    private void InitUpnp()
    {
        var sawmill = Logger.GetSawmill("net.upnp");
        var port = Port;

        var peers = _netPeers.Select(p => p.Peer).Where(p => p.Configuration.EnableUPnP).ToArray();
        if (peers.Length == 0)
        {
            sawmill.Warning("Can't UPnP forward: No IPv4-compatible NetPeers available.");
            return;
        }

        // We DON'T want to hold up the main server on this!
        new Thread(() =>
        {
            try
            {
                foreach (var peer in peers)
                {
                    // The way the NetUPnP code is written, we're doing guesswork anyway
                    // It seems to be that the assumption in regards to IPv6 is "what?" w/ UPnP????
                    // ATTENTION FUTURE 20KDC (or anyone else who comes by):
                    // IF YOU GET IPv6 FOR REALSIES, WORK OUT HOW TO DEAL W/ THIS!
                    var upnp = peer.UPnP;
                    while (upnp.Status == UPnPStatus.Discovering)
                    {
                        // Sleep while the network thread does the work
                        NetUtility.Sleep(250);
                    }

                    // Clear these forwarding rules because we don't want any OTHER SS14 servers on our network (or different local IP addresses of ourself) conflicting
                    upnp.DeleteForwardingRule(port, "UDP");
                    upnp.DeleteForwardingRule(port, "TCP");
                    var udpRes = upnp.ForwardPort(port, "RobustToolbox UDP", 0, "UDP");
                    var tcpRes = upnp.ForwardPort(port, "RobustToolbox TCP", 0, "TCP");
                    // Message needs to show in warning if something went wrong
                    var message = $"UPnP setup for port {port} on peer {peer.Configuration.LocalAddress} results: TCP {tcpRes}, UDP {udpRes}";
                    if (tcpRes && udpRes)
                    {
                        sawmill.Info(message);
                    }
                    else
                    {
                        sawmill.Warning(message);
                    }
                }
            }
            catch (Exception e)
            {
                sawmill.Warning($"UPnP threw an exception: {e}");
            }
        }).Start();
    }

    private static bool UpnpCompatible(NetPeerConfiguration cfg)
    {
        return cfg.LocalAddress.AddressFamily == AddressFamily.InterNetwork || cfg.DualStack;
    }
}
