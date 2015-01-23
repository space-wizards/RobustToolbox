using SS14.Shared.Objectives;

namespace SS14.Server.Interfaces.Objectives
{
    public interface IObjective
    {
        string ObjectiveTitle { get; set; }
        string ObjectiveDescription { get; set; }
        ObjectiveType Type { get; set; }
    }
}