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

    /// <summary>
    /// Tags defined in engine need to be instantiated here because of sandboxing
    /// </summary>
    private readonly Dictionary<string, IMarkupTag> _markupTagTypes = new()
    {
        {"color", new ColorTag()},
        {"cmdlink", new CommandLinkTag()},
        {"font", new FontTag()},
        {"bold", new BoldTag()},
        {"italic", new ItalicTag()}
    };

    /// <summary>
    /// A list of <see cref="IMarkupTag"/> types that shouldn't be instantiated through reflection
    /// </summary>
    private readonly List<Type> _engineTypes = new()
    {
        typeof(ColorTag),
        typeof(CommandLinkTag),
        typeof(FontTag),
        typeof(BoldTag),
        typeof(ItalicTag)
    };

    public void Initialize()
    {
        foreach (var type in _reflectionManager.GetAllChildren<IMarkupTag>())
        {
            //Prevent tags defined inside engine from being instantiated
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
