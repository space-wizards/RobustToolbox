using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using ItemJustification = Robust.Client.UserInterface.Controls.WrapContainer.ItemJustification;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabWrapContainer : Control
    {
        private readonly CheckBox _equalSizeBox;
        private readonly CheckBox _reverseBox;
        private readonly OptionButton _axisButton;
        private readonly OptionButton _justifyButton;
        private readonly LineEdit _separationEdit;
        private readonly LineEdit _crossSeparationEdit;

        public TabWrapContainer()
        {
            var container = new WrapContainer
            {
                MouseFilter = MouseFilterMode.Stop,
                VerticalExpand = true,
            };

            var random = new Random(3005);

            for (var i = 0; i < 35; i++)
            {
                var val = random.Next(1, 16);

                var text = string.Create(val, 0, (span, _) => span.Fill('O'));
                container.AddChild(new Button { Text = text });
            }

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        SeparationOverride = 4,
                        Children =
                        {
                            (_equalSizeBox = new CheckBox
                            {
                                Text = nameof(WrapContainer.EqualSize)
                            }),
                            (_reverseBox = new CheckBox
                            {
                                Text = nameof(WrapContainer.Reverse)
                            }),
                            (_axisButton = new OptionButton()),
                            (_justifyButton = new OptionButton()),
                            (_separationEdit = new LineEdit
                            {
                                PlaceHolder = "Separation",
                                SetWidth = 100,
                            }),
                            (_crossSeparationEdit = new LineEdit
                            {
                                PlaceHolder = "Cross Separation",
                                SetWidth = 100,
                            })
                        }
                    },
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Black },
                        Children =
                        {
                            container
                        }
                    }
                },
            });

            _axisButton.AddItem(nameof(Axis.Horizontal), (int)Axis.Horizontal);
            _axisButton.AddItem(nameof(Axis.HorizontalReverse), (int)Axis.HorizontalReverse);
            _axisButton.AddItem(nameof(Axis.Vertical), (int)Axis.Vertical);
            _axisButton.AddItem(nameof(Axis.VerticalReverse), (int)Axis.VerticalReverse);

            _axisButton.OnItemSelected += args =>
            {
                _axisButton.SelectId(args.Id);
                container.LayoutAxis = (Axis)args.Id;
            };

            _justifyButton.AddItem(nameof(ItemJustification.Begin), (int)ItemJustification.Begin);
            _justifyButton.AddItem(nameof(ItemJustification.Center), (int)ItemJustification.Center);
            _justifyButton.AddItem(nameof(ItemJustification.End), (int)ItemJustification.End);

            _justifyButton.OnItemSelected += args =>
            {
                _justifyButton.SelectId(args.Id);
                container.Justification = (ItemJustification)args.Id;
            };

            _equalSizeBox.OnPressed += _ => container.EqualSize = _equalSizeBox.Pressed;
            _reverseBox.OnPressed += _ => container.Reverse = _reverseBox.Pressed;

            _separationEdit.OnTextChanged += args =>
            {
                if (!int.TryParse(args.Text, out var sep))
                    sep = 0;

                container.SeparationOverride = sep;
            };

            _crossSeparationEdit.OnTextChanged += args =>
            {
                if (!int.TryParse(args.Text, out var sep))
                    sep = 0;

                container.CrossSeparationOverride = sep;
            };
        }
    }
}
