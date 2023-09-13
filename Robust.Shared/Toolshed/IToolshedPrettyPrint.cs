using System.Collections;

namespace Robust.Shared.Toolshed;

public interface IToolshedPrettyPrint
{
    public string PrettyPrint(ToolshedManager toolshed, out IEnumerable? more, bool moreUsed = false,int? maxOutput = null);
}
