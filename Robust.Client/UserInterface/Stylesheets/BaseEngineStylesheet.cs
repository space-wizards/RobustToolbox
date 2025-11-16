using System;
using System.Collections.Generic;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.UserInterface.Stylesheets;


public abstract class BaseEngineStylesheet
{
    [Dependency] protected readonly IReflectionManager ReflectionManager = null!;
    [Dependency] public readonly IResourceCache Resources = null!;

    internal record NoConfig;

    private object _config;

    public Stylesheet Stylesheet { get; init; }

    /// <remarks>
    ///     This constructor will not access any virtual or abstract properties, so you can set them from your config.
    /// </remarks>
    private protected BaseEngineStylesheet(object config)
    {
        IoCManager.InjectDependencies(this);
        _config = config;
        Stylesheet = null!;
    }

    public StyleRule[] GetSheetletRules<TSheetTy>(Type sheetletTy)
    {
        EngineSheetlet<TSheetTy>? sheetlet = null;
        try
        {
            if (sheetletTy.ContainsGenericParameters)
            {
                if (Activator.CreateInstance(sheetletTy.MakeGenericType(typeof(TSheetTy))) is EngineSheetlet<TSheetTy>
                    sheetlet1)
                    sheetlet = sheetlet1;
            }
            else if (Activator.CreateInstance(sheetletTy) is EngineSheetlet<TSheetTy> sheetlet2)
            {
                sheetlet = sheetlet2;
            }
        }
        // thrown when `sheetletTy.MakeGenericType` is given a type that does not satisfy the type constraints of
        // `sheetletTy`
        catch (ArgumentException) { }

        return sheetlet?.GetRules((TSheetTy)(object)this, _config) ?? [];
    }

    public StyleRule[] GetAllSheetletRules<TSheetTy, TAttrib>()
        where TAttrib : Attribute
    {
        var tys = ReflectionManager.FindTypesWithAttribute<TAttrib>();
        var rules = new List<StyleRule>();

        foreach (var ty in tys)
        {
            rules.AddRange(GetSheetletRules<TSheetTy>(ty));
        }

        return rules.ToArray();
    }
}
