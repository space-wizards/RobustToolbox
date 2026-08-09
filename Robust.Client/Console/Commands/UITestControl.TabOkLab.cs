using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabOkLab : Control
    {
        public TabOkLab()
        {
            string[] colors = ["#d52800", "#fd9954", "#ffffff", "#d261a3", "#a30061"];

            var controls = new List<ColorControl>();
            var box = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };

            foreach (var c in colors)
            {
                var control = new ColorControl(c);
                controls.Add(control);
                box.AddChild(control);
            }

            var slider = new Slider
            {
                MaxValue = 1,
                Value = 0.85f,
            };

            var slider2 = new Slider
            {
                MaxValue = 1,
                Value = 1,
            };

            slider.OnValueChanged += _ => UpdateValue();
            slider2.OnValueChanged += _ => UpdateValue();
            UpdateValue();

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    slider,
                    slider2,
                    box
                }
            });

            return;

            void UpdateValue()
            {
                foreach (var control in controls)
                {
                    var sb = new StringBuilder();

                    var origLab = Color.ToLab(Color.FromSrgb(control.OrigColor));

                    sb.AppendLine($"ORIG:\n{control.OrigColor.ToHex()}");
                    sb.AppendLine($"LAB:\n{origLab}");

                    var lch = Color.ToLch(origLab);
                    sb.AppendLine($"LCH:\n{lch}");

                    lch = Color.ToLch(origLab);
                    lch.X *= slider.Value;
                    lch.Y *= slider2.Value;

                    var newLab = Color.FromLch(lch);

                    var newColor = Color.ToSrgb(Color.FromLab(newLab));

                    sb.AppendLine($"NEW:\n{newColor.ToHex()}");
                    sb.AppendLine($"LAB:\n{newLab}");
                    sb.AppendLine($"LCH:\n{lch}");

                    control.DataLabel.Text = sb.ToString();

                    var newLab2 = Vector4.Lerp(new Vector4(0, 0, 0, 1), origLab, slider.Value);

                    control.PanelModifiedLightnessNaive.BackgroundColor = newColor;
                    control.PanelModifiedScaleAll.BackgroundColor = Color.ToSrgb(Color.FromLab(newLab2));
                }
            }
        }

        private sealed class ColorControl : Control
        {
            public readonly StyleBoxFlat PanelModifiedLightnessNaive = new();
            public readonly StyleBoxFlat PanelModifiedScaleAll = new();
            public readonly Label DataLabel = new();
            public readonly Color OrigColor;

            public ColorControl(string color)
            {
                HorizontalExpand = true;

                OrigColor = Color.FromHex(color);

                var panelSize = new Vector2(200, 200);

                AddChild(new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children =
                    {
                        new AspectRatioPanel
                        {
                            Children =
                            {
                                new PanelContainer
                                {
                                    PanelOverride = new StyleBoxFlat(OrigColor),
                                    MinSize = panelSize
                                },
                            }
                        },
                        DataLabel,
                        new AspectRatioPanel
                        {
                            Children =
                            {
                                new PanelContainer
                                {
                                    PanelOverride = PanelModifiedLightnessNaive,
                                    MinSize = panelSize
                                },
                            }
                        },
                        new AspectRatioPanel
                        {
                            Children =
                            {
                                new PanelContainer
                                {
                                    PanelOverride = PanelModifiedScaleAll,
                                    MinSize = panelSize
                                },
                            }
                        },
                    }
                });
            }
        }
    }
}
