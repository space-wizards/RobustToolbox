using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    private readonly Dictionary<string, IMarkupTag> _markupTagTypes = new IMarkupTag[] {
        new BoldItalicTag(),
        new BoldTag(),
        new BulletTag(),
        new ColorTag(),
        new CommandLinkTag(),
        new FontTag(),
        new HeadingTag(),
        new ItalicTag()
    }.ToDictionary(x => x.Name.ToLower(), x => x);

    /// <summary>
    /// A list of <see cref="IMarkupTag"/> types that shouldn't be instantiated through reflection
    /// </summary>
    private readonly List<Type> _engineTypes = new()
    {
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(CommandLinkTag),
        typeof(FontTag),
        typeof(HeadingTag),
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
            _markupTagTypes[instance.Name.ToLower()] = instance;
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

    public bool TryGetMarkupTag(string name, Type[]? tagsAllowed, [NotNullWhen(true)] out IMarkupTag? tag)
    {
        if (_markupTagTypes.TryGetValue(name, out var markupTag)
            // Using a whitelist prevents new tags from sneaking in.
            && (tagsAllowed == null || Array.IndexOf(tagsAllowed, markupTag.GetType()) != -1))
        {
            tag = markupTag;
            return true;
        }

        tag = null;
        return false;
    }
}
