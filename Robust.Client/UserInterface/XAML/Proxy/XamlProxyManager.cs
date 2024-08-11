using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using RobustXaml;
using XamlX.IL;

namespace Robust.Client.UserInterface.XAML.Proxy;

public interface IXamlProxyManager
{
    void Initialize();
    bool CanSetImplementation(string fileName);
    void SetImplementation(string fileName, string fileContent);
    bool Populate(Type t, object o);
}

public sealed class XamlProxyManager: IXamlProxyManager
{
    ISawmill _sawmill = null!;
    [Dependency] IReflectionManager _reflectionManager = null!;
    [Dependency] ILogManager _logManager = null!;

    ImplementationStorage _implementationStorage = null!;

    List<Assembly> _knownAssemblies = [];
    XamlJitCompiler? _xamlJitCompiler = null;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("xamlproxy");
        _implementationStorage = new ImplementationStorage(_sawmill, Compile);

        AddAssemblies();
        _reflectionManager.OnAssemblyAdded += (_, _) => { AddAssemblies(); };
    }

    public bool CanSetImplementation(string fileName)
    {
#if !DEBUG
        return false;
#endif

        return _implementationStorage.CanSetImplementation(fileName);
    }

    public void SetImplementation(string fileName, string fileContent)
    {
#if !DEBUG
        return
#endif
        _implementationStorage.SetImplementation(fileName, fileContent);
    }

    private void AddAssemblies()
    {
#if !DEBUG
        return;
#endif
        foreach (var a in _reflectionManager.Assemblies)
        {
            if (!_knownAssemblies.Contains(a))
            {
                _knownAssemblies.Add(a);
                _implementationStorage.Add(a);

                _xamlJitCompiler = null;
            }
        }

        // Always use the JITed versions on debug builds
        _implementationStorage.ForceReloadAll();
    }

    public bool Populate(Type t, object o)
    {
#if !DEBUG
        return false;
#endif
        return _implementationStorage.Populate(t, o);
    }

    MethodInfo? Compile(Type t, Uri uri, string fileName, string content)
    {
#if !DEBUG
        throw new NotImplementedException("XamlProxyManager.Compile() should never be called on a release build");
#endif
        // TODO: Prevent races
        XamlJitCompiler xjit;
        lock(this)
        {
            xjit = _xamlJitCompiler ??= new XamlJitCompiler();
        }

        var result = xjit.CompilePopulate(t, uri, fileName, content,
            (assembly) =>
            {
                // no assertions yet
                // TODO: Switch this to Cecil so we can run assertions.
            }
        );

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
