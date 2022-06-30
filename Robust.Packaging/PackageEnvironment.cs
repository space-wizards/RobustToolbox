namespace Robust.Packaging;

public sealed class PackageEnvironment
{
    private readonly Dictionary<string, string> _properties = new();
    private readonly ReaderWriterLockSlim _propertiesLock = new();

    public string GetProperty(string key)
    {
        _propertiesLock.EnterReadLock();

        try
        {
            return _properties.TryGetValue(key, out var prop) ? prop : "";
        }
        finally
        {
            _propertiesLock.ExitReadLock();
        }
    }

    public void SetProperty(string key, string value)
    {
        _propertiesLock.EnterWriteLock();

        try
        {
            _properties[key] = value;
        }
        finally
        {
            _propertiesLock.ExitWriteLock();
        }
    }
}
