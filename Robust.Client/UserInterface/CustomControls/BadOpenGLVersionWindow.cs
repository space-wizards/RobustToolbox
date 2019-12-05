using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    // I realize it's kind of ironic to use OpenGL to display this.
    // Ideally we'd use some form of OS alert box.
    // But those are harder to do.
    // And there's at least one person that would've ran into this.
    internal sealed class BadOpenGLVersionWindow : SS14Window
    {
        public BadOpenGLVersionWindow(IClydeDebugInfo debugInfo)
        {
            Title = "Unsupported OpenGL version!";

            var message = $@"You are using an old ({debugInfo.OpenGLVersion}) version of OpenGL.
The minimum version we support is {debugInfo.MinimumVersion}.

Make sure your graphics drivers are up to date.
If this does not help, your graphics card is probably too old.
You will most likely experience graphics glitches, if you can even read this in the first place.

Extra debugging info:
Renderer: {debugInfo.Renderer}
Vendor: {debugInfo.Vendor}
Version: {debugInfo.VersionString}";

            var label = new Label {Text = message};
            Contents.AddChild(label);
        }
    }
}
