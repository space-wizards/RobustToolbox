using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface.XAML.Proxy;

internal sealed class ImplementationStorage
{
    // For each filename: its last known Uri
    Dictionary<string, Uri> _fileUri = new();

    // For each filename: its last known content
    Dictionary<string, string> _fileContent = new();

    // For each XAML filename: the list of Types whose Populate implementation is
    // determined by that file's content.
    Dictionary<string, List<Type>> _fileTypes = new();

    // For each type: its Populate implementation.
    // If empty, then the AOT-compiled implementation should be used.
    // (You can signal this by returning false)
    Dictionary<Type, MethodInfo> _populateImplementations = new();

    ISawmill _sawmill;
    XamlJitDelegate _jitDelegate;

    public ImplementationStorage(ISawmill sawmill, XamlJitDelegate jitDelegate)
    {
        _sawmill = sawmill;
        _jitDelegate = jitDelegate;
    }

    public IEnumerable<(Type, XamlMetadataAttribute)> TypesWithXamlMetadata(Assembly a)
    {
        foreach (var type in a.GetTypes())
        {
            if (type.GetCustomAttribute<XamlMetadataAttribute>() is not { } attr)
            {
                continue;
            }

            yield return (type, attr);
        }

    }

    public void Add(Assembly a)
    {
        foreach (var (type, metadata) in TypesWithXamlMetadata(a))
        {
            // TODO: If loading Uri fails, complain _loud_, as that suggests the importer is broken
            // or the assembly author tried to stipulate this manually
            var uri = new Uri(metadata.Uri);
            var fileName = metadata.FileName;
            var content = metadata.Content;

            _fileUri[fileName] = uri;
            _fileContent[fileName] = content;

            List<Type> types;
            _fileTypes[fileName] = (types = _fileTypes.GetValueOrDefault(fileName) ?? []);
            types.Add(type);
        }
    }

    public void ForceReloadAll()
    {
        foreach (var (fileName, fileContent) in _fileContent)
        {
            SetImplementation(fileName, fileContent);
        }
    }

    public void SetImplementation(string fileName, string fileContent)
    {
        var uri =
            _fileUri.GetValueOrDefault(fileName) ??
            throw new NullReferenceException("file URI missing (this is a bug in ImplementationStorage)");
        if (!_fileTypes.TryGetValue(fileName, out var types))
        {
            // TODO: Log that no files were associated with this name
            return;
        }

        foreach (var type in types)
        {
            _sawmill.Debug($"hot reload: replacing {fileName} for {type}");
            var impl = _jitDelegate(type, uri, fileName, fileContent);
            if (impl != null)
            {
                _populateImplementations[type] = impl;
            }
        }
        _fileContent[fileName] = fileContent;
    }

    public bool Populate(Type t, object o)
    {
        if (!_populateImplementations.TryGetValue(t, out var implementation))
        {
            // pop out if we never JITed anything
            _sawmill.Debug($"no JITed implementation for {t}");
            return false;
        }

        _sawmill.Debug($"using JITed implementation for {t}");
        implementation.Invoke(null, [null, o]);
        return true;
    }
}
