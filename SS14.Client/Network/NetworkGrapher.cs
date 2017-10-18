using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Resource;
using SS14.Client.ResourceManagement;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Network
{
    /// <summary>
    /// UI element to display network debug info.
    /// </summary>
    public class NetworkGrapher : INetworkGrapher
    {
        private const int MaxDataPoints = 200;
        private readonly List<DataPoint> _dataPoints = new List<DataPoint>(MaxDataPoints);

        [Dependency] private readonly IClientNetManager _networkManager;

        [Dependency] private readonly IResourceCache _resourceCache;

        [Dependency] private readonly IGameTiming _timing;

        private bool _enabled;
        private TimeSpan _lastDataPointTime;
        private int _lastReceivedBytes;
        private int _lastSentBytes;
        private int _lastRecPkts;
        private int _lastSentPkts;

        private uint _lastTick;

        private TextSprite _textSprite;

        public void Initialize()
        {
            _textSprite = new TextSprite("", _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font);
            _textSprite.FillColor = Color.WhiteSmoke;
        }

        public void Toggle()
        {
            _enabled = !_enabled;

            if (!_enabled)
                return;

            _dataPoints.Clear();

            _lastTick = _timing.CurTick;
            _lastDataPointTime = _timing.RealTime;

            _lastReceivedBytes = _networkManager.Statistics.ReceivedBytes;
            _lastSentBytes = _networkManager.Statistics.SentBytes;

            _lastRecPkts = _networkManager.Statistics.SentPackets;
            _lastSentPkts = _networkManager.Statistics.ReceivedPackets;
        }

        public void Update()
        {
            if (!_enabled) return;

            while (_lastTick < _timing.CurTick)
            {
                AddDataPoint();
                _lastTick++;
            }

            DrawGraph();
        }

        private void DrawGraph()
        {
            var totalRecBytes = 0;
            var totalSentBytes = 0;
            var totalMilliseconds = 0d;
            var totalRecPkts = 0;
            var totalSentPkts = 0;

            for (var i = 0; i < MaxDataPoints; i++)
            {
                if (_dataPoints.Count <= i) continue;

                totalMilliseconds += _dataPoints[i].ElapsedMilliseconds;
                totalRecBytes += _dataPoints[i].ReceivedBytes;
                totalSentBytes += _dataPoints[i].SentBytes;
                totalRecPkts += _dataPoints[i].ReceivedPkts;
                totalSentPkts += _dataPoints[i].SentPkts;

                CluwneLib.ResetRenderTarget();

                CluwneLib.drawRectangle((int)CluwneLib.CurrentRenderTarget.Size.X - 2 * (MaxDataPoints - i) + 2,
                    (int)CluwneLib.CurrentRenderTarget.Size.Y - (int)(_dataPoints[i].SentBytes * 0.1f),
                    2,
                    (int)(_dataPoints[i].SentBytes * 0.2f),
                    new Color4(0, 128, 0, 255));

                CluwneLib.drawRectangle((int) CluwneLib.CurrentRenderTarget.Size.X - 2 * (MaxDataPoints - i),
                    (int) CluwneLib.CurrentRenderTarget.Size.Y - (int) (_dataPoints[i].ReceivedBytes * 0.1f),
                    2,
                    (int) (_dataPoints[i].ReceivedBytes * 0.2f),
                    new Color4(255, 0, 0, 128));

            }

            _textSprite.Text = string.Format("Up: {0:0.00} kb/s", Math.Round(totalSentBytes / totalMilliseconds, 2));
            _textSprite.Position = new Vector2i((int) CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 125, (int) CluwneLib.CurrentRenderTarget.Size.Y - 4 * _textSprite.Height - 5);
            _textSprite.Draw();

            _textSprite.Text = string.Format("Down: {0:0.00} kb/s", Math.Round(totalRecBytes / totalMilliseconds, 2));
            _textSprite.Position = new Vector2i((int) CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 125, (int) CluwneLib.CurrentRenderTarget.Size.Y - 3 * _textSprite.Height - 5);
            _textSprite.Draw();

            _textSprite.Text = string.Format("Out: {0} pkts", Math.Round(totalSentPkts / (totalMilliseconds / 1000)));
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 125, (int)CluwneLib.CurrentRenderTarget.Size.Y - 2 * _textSprite.Height - 5);
            _textSprite.Draw();

            _textSprite.Text = string.Format("In: {0} pkts", Math.Round(totalRecPkts / (totalMilliseconds / 1000)));
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 125, (int)CluwneLib.CurrentRenderTarget.Size.Y - 1 * _textSprite.Height - 5);
            _textSprite.Draw();

            _textSprite.Text = string.Format("Ping: {0}ms", _networkManager.ServerChannel?.Ping ?? -1);
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 0, (int)CluwneLib.CurrentRenderTarget.Size.Y - 4 * _textSprite.Height - 5);
            _textSprite.Draw();

            _textSprite.Text = string.Format("Frame Time: {0:0.00}ms ({1:0.00}FPS)", _timing.RealFrameTimeAvg.TotalMilliseconds, 1 / _timing.RealFrameTimeAvg.TotalSeconds);
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 0, (int)CluwneLib.CurrentRenderTarget.Size.Y - 3 * _textSprite.Height - 5);
            _textSprite.Draw();

            _textSprite.Text = string.Format("Frame SD: {0:0.00}ms", _timing.RealFrameTimeStdDev.TotalMilliseconds);
            _textSprite.Position = new Vector2i((int)CluwneLib.CurrentRenderTarget.Size.X - 4 * MaxDataPoints - 0, (int)CluwneLib.CurrentRenderTarget.Size.Y - 2 * _textSprite.Height - 5);
            _textSprite.Draw();
        }

        private void AddDataPoint()
        {
            if (_dataPoints.Count > MaxDataPoints) _dataPoints.RemoveAt(0);
            if (!_networkManager.IsConnected) return;

            _dataPoints.Add(new DataPoint
                (
                    _networkManager.Statistics.ReceivedBytes - _lastReceivedBytes,
                    _networkManager.Statistics.SentBytes - _lastSentBytes,
                    _networkManager.Statistics.ReceivedPackets - _lastRecPkts,
                    _networkManager.Statistics.SentPackets - _lastSentPkts,
                    (_timing.RealTime - _lastDataPointTime).TotalMilliseconds)
            );

            _lastDataPointTime = _timing.RealTime;
            _lastReceivedBytes = _networkManager.Statistics.ReceivedBytes;
            _lastSentBytes = _networkManager.Statistics.SentBytes;
            _lastRecPkts = _networkManager.Statistics.ReceivedPackets;
            _lastSentPkts = _networkManager.Statistics.SentPackets;
        }

        private struct DataPoint
        {
            public readonly double ElapsedMilliseconds;
            public readonly int ReceivedBytes;
            public readonly int SentBytes;
            public readonly int ReceivedPkts;
            public readonly int SentPkts;

            public DataPoint(int rec, int sent, int recPkts, int sentPkts, double elapsed)
            {
                ReceivedBytes = rec;
                SentBytes = sent;
                ElapsedMilliseconds = elapsed;
                ReceivedPkts = recPkts;
                SentPkts = sentPkts;

            }
        }
    }
}
