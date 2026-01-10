using Robust.Shared.Serialization;

namespace Robust.Client.Serialization;

[NotContentImplementable]
public interface IClientRobustSerializer : IRobustSerializer
{
    /// <summary>
    ///     Sets the string mappings used by <see cref="IRobustMappedStringSerializer"/>. Only supported in single
    ///     player games.
    /// </summary>
    void SetStringSerializerPackage(byte[] hash, byte[] package);
}
