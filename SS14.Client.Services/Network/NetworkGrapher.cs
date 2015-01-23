using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Client.Services.Network
{
    public class NetworkGrapher : INetworkGrapher
    {
        private const int MaxDataPoints = 200;
        private readonly List<NetworkStatisticsDataPoint> _dataPoints;
        private readonly INetworkManager _networkManager;
        private readonly IResourceManager _resourceManager;
        private readonly TextSprite _textSprite;
        private bool _enabled;
        private DateTime _lastDataPointTime;
        private int _lastRecievedBytes;
        private int _lastSentBytes;

        public NetworkGrapher(IResourceManager resourceManager, INetworkManager networkManager)
        {
            _resourceManager = resourceManager;
            _networkManager = networkManager;
            _dataPoints = new List<NetworkStatisticsDataPoint>();
            _lastDataPointTime = DateTime.Now;
            _textSprite = new TextSprite("NetGraphText", "", _resourceManager.GetFont("base_font"));
        }

        #region INetworkGrapher Members

        public void Toggle()
        {
            _enabled = !_enabled;
            if (!_enabled) return;

            _dataPoints.Clear();
            _lastDataPointTime = DateTime.Now;
            _lastRecievedBytes = _networkManager.CurrentStatistics.ReceivedBytes;
            _lastSentBytes = _networkManager.CurrentStatistics.SentBytes;
        }

        public void Update()
        {
            if (!_enabled) return;

            if ((DateTime.Now - _lastDataPointTime).TotalMilliseconds > 200)
                AddDataPoint();

            DrawGraph();
        }

        #endregion

        private void DrawGraph()
        {
            int totalRecBytes = 0;
            int totalSentBytes = 0;
            double totalMilliseconds = 0d;

            for (int i = 0; i < MaxDataPoints; i++)
            {
                if (_dataPoints.Count <= i) continue;

                totalMilliseconds += _dataPoints[i].ElapsedMilliseconds;
                totalRecBytes += _dataPoints[i].RecievedBytes;
                totalSentBytes += _dataPoints[i].SentBytes;

                Gorgon.CurrentRenderTarget = null;

                //Draw recieved line
                Gorgon.CurrentRenderTarget.Rectangle(Gorgon.CurrentRenderTarget.Width - (4*(MaxDataPoints - i)),
                                                     Gorgon.CurrentRenderTarget.Height -
                                                     (_dataPoints[i].RecievedBytes*0.1f), 2,
                                                     (_dataPoints[i].RecievedBytes*0.1f),
                                                     Color.FromArgb(180, Color.Red));

                Gorgon.CurrentRenderTarget.Rectangle(Gorgon.CurrentRenderTarget.Width - (4*(MaxDataPoints - i)) + 2,
                                                     Gorgon.CurrentRenderTarget.Height - (_dataPoints[i].SentBytes*0.1f),
                                                     2, (_dataPoints[i].SentBytes*0.1f),
                                                     Color.FromArgb(180, Color.Green));
            }

            _textSprite.Text = String.Format("Up: {0} kb/s.", Math.Round(totalSentBytes/totalMilliseconds, 6));
            _textSprite.SetPosition(Gorgon.CurrentRenderTarget.Width - (4*MaxDataPoints) - 100,
                                    Gorgon.CurrentRenderTarget.Height - 30);
            _textSprite.Draw();

            _textSprite.Text = String.Format("Down: {0} kb/s.", Math.Round(totalRecBytes/totalMilliseconds, 6));
            _textSprite.SetPosition(Gorgon.CurrentRenderTarget.Width - (4*MaxDataPoints) - 100,
                                    Gorgon.CurrentRenderTarget.Height - 60);
            _textSprite.Draw();
        }

        private void AddDataPoint()
        {
            if (_dataPoints.Count > MaxDataPoints) _dataPoints.RemoveAt(0);
            if (!_networkManager.IsConnected) return;

            _dataPoints.Add(new NetworkStatisticsDataPoint
                                (
                                _networkManager.CurrentStatistics.ReceivedBytes - _lastRecievedBytes,
                                _networkManager.CurrentStatistics.SentBytes - _lastSentBytes,
                                (DateTime.Now - _lastDataPointTime).TotalMilliseconds)
                );

            _lastDataPointTime = DateTime.Now;
            _lastRecievedBytes = _networkManager.CurrentStatistics.ReceivedBytes;
            _lastSentBytes = _networkManager.CurrentStatistics.SentBytes;
        }
    }

    public struct NetworkStatisticsDataPoint
    {
        public double ElapsedMilliseconds;
        public int RecievedBytes;
        public int SentBytes;

        public NetworkStatisticsDataPoint(int rec, int sent, double elapsed)
        {
            RecievedBytes = rec;
            SentBytes = sent;
            ElapsedMilliseconds = elapsed;
        }
    }
}