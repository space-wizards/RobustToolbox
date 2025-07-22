using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using CS = System.Runtime.CompilerServices;

namespace Robust.Client.ViewVariables.Editors;

internal sealed class VVPropEditorTuple : VVPropEditor
{
    [Dependency] private readonly IClientViewVariablesManagerInternal _viewVariables = default!;

    private bool _readOnly;
    private readonly List<object?> _tuple = [];
    private readonly List<VVPropEditor> _editors = [];
    private Type? _actualType;

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

        if (value is not CS.ITuple tuple)
            return vBoxContainer;

        // Zero-tuples exist?? I'm just not going to bother with that.
        if (tuple.Length == 0)
            return vBoxContainer;

        _actualType = value.GetType();

        // We disallow editing tuples with arity more than 7 since they would
        // a pain to construct via reflection. And no one should have tuples
        // that large. (8 is bad because last element becomes a ValueTuple<>)
        _readOnly = ReadOnly
                    || tuple.Length >= 8
                    || !IsValueTuple(_actualType); // ToTuple only supports ValueTuples

        for (var i = 0; i < tuple.Length; i++)
        {
            var editor = CreateBox(tuple[i], vBoxContainer);
            var index = i; // thanks C#
            editor.OnValueChanged += (o, reinterpret) => ValueChanged(ToTuple(o, index), reinterpret);

            _tuple.Add(tuple[i]);
            _editors.Add(editor);
        }
        return vBoxContainer;
    }

    private bool IsValueTuple(Type actualType)
    {
        if (!actualType.IsGenericType)
            return false;

        Type[] valueTupleTypes =
        [
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>)
        ];
        return valueTupleTypes.Contains(actualType.GetGenericTypeDefinition());
    }

    private CS.ITuple ToTuple(object? changed, int index)
    {
        _tuple[index] = changed;

        // I can't seem to make this work using .GetMethod.
        // If you know of a better way of doing this... please do.
        return (CS.ITuple)typeof(ValueTuple).GetMethods()
            .First(x => x is { Name: nameof(ValueTuple.Create), IsGenericMethod: true }
                        && x.GetParameters().Length == _tuple.Count)
            .MakeGenericMethod(_actualType!.GenericTypeArguments)
            .Invoke(null, _tuple.ToArray())!;
    }

    private VVPropEditor CreateBox<T>(T? entry, BoxContainer parent)
    {
        var editor = _viewVariables.PropertyFor(entry?.GetType());
        // We disallow editing of serverside-only tuples because, uh, I don't
        // know how to make it work. Presumably it'd have to be something
        // similarly cursed to what I did in ToTuple above.
        parent.AddChild(editor.Initialize(entry, _readOnly));
        return editor;
    }

    // Allow selecting, for example, dictionaries within the tuple.
    // Wait, why do you have a field with a tuple that holds a dictionary??
    public override void WireNetworkSelector(uint sessionId, object[] selectorChain)
    {
        for (var i = 0; i < _editors.Count; i++)
        {
            object[] chain = [..selectorChain, new ViewVariablesTupleIndexSelector(i)];
            _editors[i].WireNetworkSelector(sessionId, chain);
        }
    }
}
