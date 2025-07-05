using System;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorTuple<T1, T2> : VVPropEditor
{
    [Dependency] private readonly IClientViewVariablesManagerInternal _viewVariables = default!;

    private ValueTuple<T1, T2> _tuple;

    public VVPropEditorTuple()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override Control MakeUI(object? value)
    {
        var vBoxContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinSize = new Vector2(240, 0),
        };

        // We only support serializing two-ples.
        _tuple = (ValueTuple<T1, T2>)value!;

        CreateBox(_tuple.Item1, vBoxContainer)
            .OnValueChanged += (o, _) => ValueChanged(((T1)o!, _tuple.Item2));
        CreateBox(_tuple.Item2, vBoxContainer)
            .OnValueChanged += (o, _) => ValueChanged((_tuple.Item1, (T2)o!));

        return vBoxContainer;
    }

    private VVPropEditor CreateBox<T>(T entry, BoxContainer parent)
    {
        var editor = _viewVariables.PropertyFor(entry?.GetType());
        parent.AddChild(editor.Initialize(entry, ReadOnly));
        return editor;
    }
}
