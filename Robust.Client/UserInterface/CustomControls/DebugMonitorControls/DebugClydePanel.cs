using System.Text;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls
{
    internal sealed class DebugClydePanel : PanelContainer
    {
        [Dependency] private readonly IClydeInternal _clydeInternal = default!;

        private readonly Label _label;

        private readonly StringBuilder _textBuilder = new();
        private readonly char[] _textBuffer = new char[512];

        public DebugClydePanel()
        {
            IoCManager.InjectDependencies(this);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
            };

            PanelOverride.SetContentMarginOverride(StyleBox.Margin.All, 5);

            AddChild(_label = new Label());
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!VisibleInTree)
            {
                return;
            }

            _textBuilder.Clear();

            var info = _clydeInternal.DebugInfo;
            var stats = _clydeInternal.DebugStats;

            _textBuilder.AppendLine($@"Renderer: {info.Renderer}
Vendor: {info.Vendor}
Version: {info.VersionString}");

            if (info.Overriding)
                _textBuilder.Append($"Version override: {info.OpenGLVersion}\n");

            _textBuilder.Append($"Windowing: {info.WindowingApi}\n");

            _textBuilder.Append($@"Draw Calls: Cly: {stats.LastClydeDrawCalls} GL: {stats.LastGLDrawCalls}
Batches: {stats.LastBatches} Max size: ({stats.LargestBatchSize.vertices} vtx, {stats.LargestBatchSize.vertices} idx)
Lights: {stats.TotalLights}, Shadowcasting: {stats.ShadowLights}, Occluders: {stats.Occluders}, Entities: { stats.Entities}");

            _label.TextMemory = FormatHelpers.BuilderToMemory(_textBuilder, _textBuffer);
        }
    }
}
