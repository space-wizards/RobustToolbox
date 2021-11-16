using System;
using System.Threading;
using Lidgren.Network;
using Robust.Shared.Log;

namespace Robust.Server.Network
{
    internal static class UPnP
    {
        public static void Start(int port, ISawmill sawmill)
        {
            // We DON'T want to hold up the main server on this!
            new Thread(() =>
            {
                try
                {
                    // The way the NetUPnP code is written, we're doing guesswork anyway
                    // It seems to be that the assumption in regards to IPv6 is "what?" w/ UPnP????
                    // ATTENTION FUTURE 20KDC (or anyone else who comes by):
                    // IF YOU GET IPv6 FOR REALSIES, WORK OUT HOW TO DEAL W/ THIS!
                    var cfg = new NetPeerConfiguration("SS14_NetTag_ForUPnP");
                    cfg.EnableUPnP = true;
                    var peer = new NetPeer(cfg);
                    peer.Start();
                    try
                    {
                        var upnp = peer.UPnP;
                        while (upnp.Status == UPnPStatus.Discovering)
                        {
                            // Sleep while the network thread does the work
                            NetUtility.Sleep(250);
                        }
                        // Clear these forwarding rules because we don't want any OTHER SS14 servers on our network (or different local IP addresses of ourself) conflicting
                        upnp.DeleteForwardingRule(port, "TCP");
                        upnp.DeleteForwardingRule(port, "UDP");
                        var tcpRes = upnp.ForwardPort(port, "RobustToolbox TCP", 0, "TCP");
                        var udpRes = upnp.ForwardPort(port, "RobustToolbox UDP", 0, "UDP");
                        // Message needs to show in warning if something went wrong
                        var message = $"UPnP setup for port {port} results: TCP {tcpRes}, UDP {udpRes}";
                        if (tcpRes && udpRes)
                        {
                            sawmill.Info(message);
                        }
                        else
                        {
                            sawmill.Warning(message);
                        }
                    }
                    finally
                    {
                        peer.Shutdown("UPnP thread shutdown");
                    }
                }
                catch (Exception e)
                {
                    sawmill.Warning($"UPnP threw an exception: {e}");
                }
            }).Start();
        }
    }
}

