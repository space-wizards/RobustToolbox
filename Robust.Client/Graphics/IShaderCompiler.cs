using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics;

public interface IShaderCompiler
{
    ShaderCompileResultWgsl CompileToWgsl(ResPath path, IReadOnlyDictionary<string, bool> features);
}

public abstract class ShaderCompileResult
{
    public bool Success { get; }

    private protected ShaderCompileResult(bool success)
    {
        Success = success;
    }
}

public sealed class ShaderCompileResultWgsl : ShaderCompileResult
{
    public byte[] Code { get; }

    internal ShaderCompileResultWgsl(byte[] code, bool success) : base(success)
    {
        Code = code;
    }
}
