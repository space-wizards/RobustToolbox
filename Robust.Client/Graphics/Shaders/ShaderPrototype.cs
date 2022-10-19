using Robust.Client.ResourceManagement;
using Robust.Client.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Graphics
{
    [Prototype("shader")]
    public readonly struct ShaderPrototype : IPrototype
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [IncludeDataField(customTypeSerializer: typeof(ClydeShaderSerializer))]
        private readonly ShaderInstance _cachedInstance = default!;

        /// <summary>
        ///     Retrieves a ready-to-use instance of this shader.
        /// </summary>
        /// <remarks>
        ///     This instance is shared. As such, it is immutable.
        ///     Use <see cref="InstanceUnique"/> to get a mutable and unique shader instance.
        /// </remarks>
        public ShaderInstance Instance() => _cachedInstance;

        public ShaderInstance InstanceUnique() => Instance().Duplicate();
    }
}
