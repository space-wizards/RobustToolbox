using System.Diagnostics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.CustomControls;

[DebuggerDisplay("spawnbutton {" + nameof(Index) + "}")]
public sealed class EntitySpawnButton : Control
{
    public string PrototypeID => Prototype.ID;
    public EntityPrototype Prototype { get; set; } = default!;
    public Button ActualButton { get; private set; }
    public Label EntityLabel { get; private set; }
    public LayeredTextureRect EntityTextureRects { get; private set; }
    public int Index { get; set; }

    public EntitySpawnButton()
    {
        AddChild(ActualButton = new Button
        {
            ToggleMode = true,
        });

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                (EntityTextureRects = new LayeredTextureRect
                {
                    MinSize = (32, 32),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    Stretch = TextureRect.StretchMode.KeepAspectCentered,
                    CanShrink = true
                }),
                (EntityLabel = new Label
                {
                    VerticalAlignment = VAlignment.Center,
                    HorizontalExpand = true,
                    Text = "Backpack",
                    ClipText = true
                })
            }
        });
    }
}
