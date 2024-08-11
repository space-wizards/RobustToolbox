#if TOOLS
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using RobustXaml;

namespace Robust.Client.UserInterface.XAML.Proxy;

public sealed class XamlProxyManager: IXamlProxyManager
{
    ISawmill _sawmill = null!;
    [Dependency] IReflectionManager _reflectionManager = null!;
    [Dependency] ILogManager _logManager = null!;

    XamlImplementationStorage _xamlImplementationStorage = null!;

    List<Assembly> _knownAssemblies = [];
    XamlJitCompiler? _xamlJitCompiler;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("xamlproxy");
        _xamlImplementationStorage = new XamlImplementationStorage(_sawmill, Compile);

        AddAssemblies();
        _reflectionManager.OnAssemblyAdded += (_, _) => { AddAssemblies(); };
    }

    public bool CanSetImplementation(string fileName)
    {
        return _xamlImplementationStorage.CanSetImplementation(fileName);
    }

    public void SetImplementation(string fileName, string fileContent)
    {
        _xamlImplementationStorage.SetImplementation(fileName, fileContent);
    }

    private void AddAssemblies()
    {
        foreach (var a in _reflectionManager.Assemblies)
        {
            if (!_knownAssemblies.Contains(a))
            {
                _knownAssemblies.Add(a);
                _xamlImplementationStorage.Add(a);

                _xamlJitCompiler = null;
            }
        }

        // Always use the JITed versions on debug builds
        _xamlImplementationStorage.ForceReloadAll();
    }

    public bool Populate(Type t, object o)
    {
        return _xamlImplementationStorage.Populate(t, o);
    }

    MethodInfo? Compile(Type t, Uri uri, string fileName, string content)
    {
        XamlJitCompiler xjit;
        lock(this)
        {
            xjit = _xamlJitCompiler ??= new XamlJitCompiler();
        }

        var result = xjit.CompilePopulate(t, uri, fileName, content);

        if (result is XamlJitCompilerResult.Error e)
        {
            _sawmill.Warning($"hot reloading-related error: {t.FullName}; {fileName}; {e.Raw} {e.Hint ?? ""}");
            return null;
        }

        if (result is XamlJitCompilerResult.Success s)
        {
            return s.MethodInfo;
        }

        throw new InvalidOperationException($"totally unexpected result from compiler operation: {result}");
    }
}
#endif
