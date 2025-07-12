using System;
using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorTuple<T1, T2> : VVPropEditor
{
    [Dependency] private readonly IClientViewVariablesManagerInternal _viewVariables = default!;

    private bool _networked;
    private ValueTuple<T1, T2> _tuple;
    private VVPropEditor? _item1Editor;
    private VVPropEditor? _item2Editor;

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

        // We only support serializing two-ples, though this could be a value
        // reference token, so it's dynamic time. (Okay yes it could be conditionally cast.)
        dynamic? val = value;
        _tuple = (val!.Item1, val.Item2);


        _networked = value is ViewVariablesBlobMembers.ServerTupleToken;

        _item1Editor = CreateBox(_tuple.Item1, vBoxContainer);
        _item2Editor = CreateBox(_tuple.Item2, vBoxContainer);

        if (_networked) return vBoxContainer;

        _item1Editor.OnValueChanged += (o, reinterpret) => ValueChanged(((T1)o!, _tuple.Item2), reinterpret);
        _item2Editor.OnValueChanged += (o, reinterpret) => ValueChanged((_tuple.Item1, (T2)o!), reinterpret);

        return vBoxContainer;
    }

    private VVPropEditor CreateBox<T>(T? entry, BoxContainer parent)
    {
        var editor = _viewVariables.PropertyFor(entry?.GetType());
        // We disallow editing of serverside-only tuples because, uh, I don't know how to make it work.
        parent.AddChild(editor.Initialize(entry, ReadOnly || _networked));
        return editor;
    }

    // Allow selecting, for example, dictionaries within the tuple.
    // Wait, why do you have a field with a tuple that holds a dictionary??
    public override void WireNetworkSelector(uint sessionId, object[] selectorChain)
    {
        var item1Selector = new ViewVariablesEnumerableIndexSelector(0);
        var item2Selector = new ViewVariablesEnumerableIndexSelector(1);

        var itemOneChain = selectorChain.Append(item1Selector).ToArray();
        var itemTwoChain = selectorChain.Append(item2Selector).ToArray();

        _item1Editor?.WireNetworkSelector(sessionId, itemOneChain);
        _item2Editor?.WireNetworkSelector(sessionId, itemTwoChain);
    }
}
