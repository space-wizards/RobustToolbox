using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;

namespace SS14.Client.Network
{
    public class NetworkGrapher : INetworkGrapher
    {
        private const int MaxDataPoints = 200;
        private readonly List<NetworkStatisticsDataPoint> _dataPoints = new List<NetworkStatisticsDataPoint>();
        [Dependency]
        private readonly IClientNetManager _networkManager;
        [Dependency]
        private readonly IResourceManager _resourceManager;
        private TextSprite _textSprite;
        private bool _enabled;
        private DateTime _lastDataPointTime;
        private int _lastRecievedBytes;
        private int _lastSentBytes;

        public NetworkGrapher()
        {
            _lastDataPointTime = DateTime.Now;
        }

        public void Initialize()
        {
            _textSprite = new TextSprite("NetGraphText", "", _resourceManager.GetFont("CALIBRI"));
        }

        #region INetworkGrapher Members

        public void Toggle()
        {
            _enabled = !_enabled;
            if (!_enabled) return;

            _dataPoints.Clear();
            _lastDataPointTime = DateTime.Now;
            _lastRecievedBytes = _networkManager.Statistics.ReceivedBytes;
            _lastSentBytes = _networkManager.Statistics.SentBytes;
        }

        public void Update()
        {
            if (!_enabled) return;

            if ((DateTime.Now - _lastDataPointTime).TotalMilliseconds > 200)
                AddDataPoint();

            DrawGraph();
        }

        #endregion INetworkGrapher Members

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

                CluwneLib.ResetRenderTarget();

                //Draw recieved line
                CluwneLib.drawRectangle((int)CluwneLib.CurrentRenderTarget.Size.X - (4 * (MaxDataPoints - i)),
                                        (int)CluwneLib.CurrentRenderTarget.Size.Y - (int)(_dataPoints[i].RecievedBytes * 0.1f),
                                        2,
                                        (int)(_dataPoints[i].RecievedBytes * 0.1f),
                                        SFML.Graphics.Color.Red.WithAlpha(180));

                CluwneLib.drawRectangle((int)CluwneLib.CurrentRenderTarget.Size.X - (4 * (MaxDataPoints - i)) + 2,
                                        (int)CluwneLib.CurrentRenderTarget.Size.Y - (int)(_dataPoints[i].SentBytes * 0.1f),
                                        2,
                                        (int)(_dataPoints[i].SentBytes * 0.1f),
                                        new SFML.Graphics.Color(0, 128, 0).WithAlpha(180));
            }

            _textSprite.Text = String.Format("Up: {0} kb/s.", Math.Round(totalSentBytes / totalMilliseconds, 6));
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - (4 * MaxDataPoints) - 100, (int)CluwneLib.CurrentRenderTarget.Size.Y - 30);
            _textSprite.Draw();

            _textSprite.Text = String.Format("Down: {0} kb/s.", Math.Round(totalRecBytes / totalMilliseconds, 6));
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - (4 * MaxDataPoints) - 100, (int)CluwneLib.CurrentRenderTarget.Size.Y - 60);
            _textSprite.Draw();
        }

        private void AddDataPoint()
        {
            if (_dataPoints.Count > MaxDataPoints) _dataPoints.RemoveAt(0);
            if (!_networkManager.IsConnected) return;

            _dataPoints.Add(new NetworkStatisticsDataPoint
                                (
                                _networkManager.Statistics.ReceivedBytes - _lastRecievedBytes,
                                _networkManager.Statistics.SentBytes - _lastSentBytes,
                                (DateTime.Now - _lastDataPointTime).TotalMilliseconds)
                );

            _lastDataPointTime = DateTime.Now;
            _lastRecievedBytes = _networkManager.Statistics.ReceivedBytes;
            _lastSentBytes = _networkManager.Statistics.SentBytes;
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
