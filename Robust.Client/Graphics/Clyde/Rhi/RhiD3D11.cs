using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed partial class RhiD3D11 : RhiBase
{
    private readonly Clyde _clyde;
    private readonly ISawmill _sawmill;

    public RhiD3D11(Clyde clyde, IDependencyCollection dependencies)
    {
        var logMgr = dependencies.Resolve<ILogManager>();

        _clyde = clyde;
        _sawmill = logMgr.GetSawmill("clyde.rhi.d3d11");
    }

    public override void Init()
    {
        _sawmill.Info("Initializing D3D11 RHI!");

    }
}
