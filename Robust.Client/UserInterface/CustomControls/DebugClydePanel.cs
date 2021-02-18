using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal sealed class DebugClydePanel : PanelContainer
    {
        [Dependency] private readonly IClydeInternal _clydeInternal = default!;

        private readonly Label _label;

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

            var info = _clydeInternal.DebugInfo;
            var stats = _clydeInternal.DebugStats;

            var overridingText = "";
            if (info.Overriding)
            {
                overridingText = $"\nVersion override: {info.OpenGLVersion}";
            }

            _label.Text = $@"Renderer: {info.Renderer}
Vendor: {info.Vendor}
Version: {info.VersionString}{overridingText}
Draw Calls: Cly: {stats.LastClydeDrawCalls} GL: {stats.LastGLDrawCalls}
Batches: {stats.LastBatches} Max size: {stats.LargestBatchSize}
Lights: {stats.TotalLights}";
        }
    }
}
