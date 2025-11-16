using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.UserInterface.Stylesheets;

public abstract class CommonEngineStylesheet : BaseEngineStylesheet
{
    private protected CommonEngineStylesheet(object config) : base(config)
    {
    }

    internal StyleRule[] GetFontRules()
    {
        var notoSansFont = Resources.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf");
        var notoSansFont12 = new VectorFont(notoSansFont, 12);

        return
        [
            Element()
                .Prop("font", notoSansFont12)
                .Prop("font-color", Color.White),
        ];
    }
}
