namespace SS14.Server.Interfaces.Objectives
{
    public interface IObjectiveSet
    {
        IObjective[] GetObjectives();
        void AddObjective(IObjective objective);
        void RemoveObjective(IObjective objective);
    }
}