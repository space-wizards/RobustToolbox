namespace Robust.Shared.Interfaces.Configuration
{
    internal interface IConfigurationManagerInternal : IConfigurationManager
    {
        T GetSecureCVar<T>(string name);
    }
}
