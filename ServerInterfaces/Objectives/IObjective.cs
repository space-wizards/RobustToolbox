using SS13_Shared.Objectives;
namespace ServerInterfaces.Objectives
{
    public interface IObjective
    {
        string ObjectiveTitle { get; set; }
        string ObjectiveDescription { get; set; }
        ObjectiveType Type { get; set; }
    }
}
