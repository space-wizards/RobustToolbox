using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.Modules.Network
{
    public class NetworkGrapher
    {
        private NetworkManager nm;
        private List<NetworkStatisticsDataPoint> dataPoints;
        private int lastRecievedBytes;
        private int lastSentBytes;
        private DateTime lastDataPointTime;
        private int maxDataPoints = 200;

        private bool enabled = true;

        public NetworkGrapher(NetworkManager _nm)
        {
            nm = _nm;
            dataPoints = new List<NetworkStatisticsDataPoint>();
            
        }

        public void Toggle()
        {
            enabled = !enabled;
            if (enabled)
            {
                dataPoints.Clear();
                lastDataPointTime = DateTime.Now;
                lastRecievedBytes = nm.netClient.Statistics.ReceivedBytes;
                lastSentBytes = nm.netClient.Statistics.SentBytes;
            }
        }

        public void Update()
        {
            if (!enabled)
                return;
            if ((DateTime.Now - lastDataPointTime).TotalMilliseconds > 200)
                AddDataPoint();
            DrawGraph();

        }

        private void DrawGraph()
        {
            for (int i = 0; i < maxDataPoints; i++)
            {
                if (dataPoints.Count <= i)
                    return;
                Gorgon.CurrentRenderTarget = null;
                //Draw recieved line
                Gorgon.CurrentRenderTarget.Rectangle(Gorgon.CurrentRenderTarget.Width - (4 * (maxDataPoints - i)),
                    Gorgon.CurrentRenderTarget.Height - (dataPoints[i].recievedBytes * 0.1f), 2, (dataPoints[i].recievedBytes * 0.1f),
                    System.Drawing.Color.FromArgb(180, System.Drawing.Color.Red));

                Gorgon.CurrentRenderTarget.Rectangle(Gorgon.CurrentRenderTarget.Width - (4 * (maxDataPoints - i)) + 2,
                    Gorgon.CurrentRenderTarget.Height - (dataPoints[i].sentBytes * 0.1f), 2, (dataPoints[i].sentBytes * 0.1f),
                    System.Drawing.Color.FromArgb(180, System.Drawing.Color.Green));
            }
        }

        private void AddDataPoint()
        {
            if (dataPoints.Count > maxDataPoints)
                dataPoints.RemoveAt(0);
            if (nm.isConnected)
            {
                dataPoints.Add(new NetworkStatisticsDataPoint(
                    nm.netClient.Statistics.ReceivedBytes - lastRecievedBytes,
                    nm.netClient.Statistics.SentBytes - lastSentBytes,
                    (DateTime.Now - lastDataPointTime).TotalMilliseconds));
                lastDataPointTime = DateTime.Now;
                lastRecievedBytes = nm.netClient.Statistics.ReceivedBytes;
                lastSentBytes = nm.netClient.Statistics.SentBytes;
            }
        }
    }

    public struct NetworkStatisticsDataPoint
    {
        public int recievedBytes;
        public int sentBytes;
        public double elapsedMilliseconds;

        public NetworkStatisticsDataPoint(int rec, int sent, double elapsed)
        {
            recievedBytes = rec;
            sentBytes = sent;
            elapsedMilliseconds = elapsed;
        }
    }
}
