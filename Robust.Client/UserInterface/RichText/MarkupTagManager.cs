using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Robust.Client.UserInterface.RichText;

public sealed class MarkupTagManager
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;

    private readonly Dictionary<string, IMarkupTag> _markupTagTypes = new()
    {
        {"color", new ColorTag()},
        {"cmdLink", new CommandLinkTag()}
    };

    private readonly List<Type> _engineTypes = new()
    {
        typeof(ColorTag),
        typeof(CommandLinkTag)
    };

    public void Initialize()
    {
        foreach (var type in _reflectionManager.GetAllChildren<IMarkupTag>())
        {
            if (_engineTypes.Contains(type))
                continue;

            var instance = (IMarkupTag)_sandboxHelper.CreateInstance(type);
            _markupTagTypes.Add(instance.Name.ToLower(),  instance);
        }

        foreach (var (_, tag) in _markupTagTypes)
        {
            IoCManager.InjectDependencies(tag);
        }
    }

    public IMarkupTag? GetMarkupTag(string name)
    {
        return _markupTagTypes.GetValueOrDefault(name);
    }

    public bool TryGetMarkupTag(string name, [NotNullWhen(true)] out IMarkupTag? tag)
    {
        if (_markupTagTypes.TryGetValue(name, out var markupTag))
        {
            tag = markupTag;
            return true;
        }

        tag = null;
        return false;
    }
}
