using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls;

public sealed partial class RichTextBox : BoxContainer
{
    [Dependency] private readonly IResourceCache _resourceCache = null!;

    private string _content = string.Empty;

    public string Content
    {
        get => _content;
        set
        {
            SetContent(value);
            _content = value;
        }
    }

    public RichTextBox()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;
        HorizontalAlignment = HAlignment.Stretch;
        HorizontalExpand = true;
    }

    public void SetContent(string content)
    {
        _content = content;
        var document = TryParse(content);

        Children.Clear();

        if (document is null)
        {
            var label = new Label() { Text = content, FontColorOverride = Color.Black };
            AddChild(label);
            return;
        }

        var nodes = new Stack<(Node, Control)>(
            (document.Children.Select(n => (n, (Control)this)).Reverse()));

        while (nodes.Count != 0)
        {
            var (node, parent) = nodes.Pop();

            switch (node)
            {
                case TextNode textNode:
                {
                    var label = new Label()
                    {
                        Text = textNode.Text,
                        FontColorOverride = Color.Black,
                        HorizontalExpand = false,
                        VerticalExpand = false,
                        HorizontalAlignment = HAlignment.Left,
                    };
                    parent.AddChild(label);
                    break;
                }
                case TagNode tagNode:
                    switch (tagNode.Name)
                    {
                        case "center":
                            var centeredBox = new BoxContainer()
                            {
                                HorizontalExpand = true,
                                HorizontalAlignment = HAlignment.Center,
                                Orientation = LayoutOrientation.Vertical,
                            };

                            parent.AddChild(centeredBox);

                            PushChildren(tagNode.Children, nodes, centeredBox);

                            break;

                        case "right":
                            var rightBox = new BoxContainer()
                            {
                                HorizontalExpand = true,
                                HorizontalAlignment = HAlignment.Right,
                                Orientation = LayoutOrientation.Vertical,
                            };

                            parent.AddChild(rightBox);

                            PushChildren(tagNode.Children, nodes, rightBox);

                            break;

                        case "flow":
                            var flowBox = new BoxContainer()
                            {
                                HorizontalExpand = true,
                                HorizontalAlignment = HAlignment.Left,
                            };

                            parent.AddChild(flowBox);

                            PushChildren(tagNode.Children, nodes, flowBox);

                            break;

                        case "logo":
                            var texture =
                                _resourceCache.GetResource<TextureResource>("/Textures/Interface/Nano/ntlogo.svg.png");

                            var image = new TextureRect()
                            {
                                Texture = texture,
                                Stretch = TextureRect.StretchMode.Scale,
                                Modulate = Color.Black,
                                HorizontalExpand = false,
                                HorizontalAlignment = HAlignment.Left,
                            };

                            if (tagNode.Attributes.FirstOrDefault(attr => attr.Name == "size") is { } sizeAttr &&
                                float.TryParse(sizeAttr.Value, out var size))
                            {
                                image.SetSize = new Vector2(size);
                            }

                            parent.AddChild(image);

                            break;
                    }

                    break;
            }
        }
    }

    private static void PushChildren(IReadOnlyList<Node> children, Stack<(Node, Control)> context, Control parent)
    {
        foreach (var n in children.Reverse())
        {
            context.Push((n, parent));
        }
    }
}
