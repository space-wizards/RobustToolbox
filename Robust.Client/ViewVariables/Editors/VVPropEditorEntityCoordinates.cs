using System.Globalization;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors
{
    public sealed class VVPropEditorEntityCoordinates : VVPropEditor
    {
        protected override Control MakeUI(object? value)
        {
            var coords = (EntityCoordinates) value!;
            var hBoxContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                MinSize = new Vector2(240, 0),
            };

            hBoxContainer.AddChild(new Label {Text = "grid: "});

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var gridId = new LineEdit
            {
                Editable = !ReadOnly,
                HorizontalExpand = true,
                PlaceHolder = "Grid ID",
                ToolTip = "Grid ID",
                Text = coords.GetGridUid(entityManager)?.ToString() ?? ""
            };

            hBoxContainer.AddChild(gridId);

            hBoxContainer.AddChild(new Label {Text = "pos: "});

            var x = new LineEdit
            {
                Editable = !ReadOnly,
                HorizontalExpand = true,
                PlaceHolder = "X",
                ToolTip = "X",
                Text = coords.X.ToString(CultureInfo.InvariantCulture)
            };

            hBoxContainer.AddChild(x);

            var y = new LineEdit
            {
                Editable = !ReadOnly,
                HorizontalExpand = true,
                PlaceHolder = "Y",
                ToolTip = "Y",
                Text = coords.Y.ToString(CultureInfo.InvariantCulture)
            };

            hBoxContainer.AddChild(y);

            void OnEntered(LineEdit.LineEditEventArgs e)
            {
                var gridVal = EntityUid.Parse(gridId.Text);
                var mapManager = IoCManager.Resolve<IMapManager>();
                var xVal = float.Parse(x.Text, CultureInfo.InvariantCulture);
                var yVal = float.Parse(y.Text, CultureInfo.InvariantCulture);

                if (!entityManager.HasComponent<MapGridComponent>(gridVal))
                {
                    ValueChanged(new EntityCoordinates(EntityUid.Invalid, new(xVal, yVal)));
                    return;
                }

                ValueChanged(new EntityCoordinates(gridVal, new(xVal, yVal)));
            }

            if (!ReadOnly)
            {
                gridId.OnTextEntered += OnEntered;
                x.OnTextEntered += OnEntered;
                y.OnTextEntered += OnEntered;
            }

            return hBoxContainer;
        }
    }
}
