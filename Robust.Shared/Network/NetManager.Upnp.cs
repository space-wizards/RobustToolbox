using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Lidgren.Network;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

public partial class NetManager
{
    private void InitUpnp()
    {
        var sawmill = _logMan.GetSawmill("net.upnp");
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
                    // There was previously a comment here from 20kdc wondering how IPv6 works with UPnP.
                    // The answer is that the relevant UPnP service for IPv6 is "WANIPv6FirewallControl".
                    // My router doesn't support it, so I can't test or implement it.
                    var upnp = peer.UPnP;
                    DebugTools.Assert(upnp != null);
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
                    if (!udpRes)
                        sawmill.Error($"Peer {peer.Configuration.LocalAddress}: Failed to UPnP port forward {port}/udp");

                    if (!tcpRes)
                        sawmill.Error($"Peer {peer.Configuration.LocalAddress}: Failed to UPnP port forward {port}/tcp");

                    if (tcpRes && udpRes)
                    {
                        sawmill.Info($"Peer {peer.Configuration.LocalAddress}: Successfully UPnP port forwarded {port}/udp and {port}/tcp");
                    }
                    else
                    {
                        sawmill.Warning($"Peer {peer.Configuration.LocalAddress}: Failed UPnP port forwarding, " +
                                        "your server may not be accessible. " +
                                        "Check with your router's settings to enable UPnP or port forward manually");
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
