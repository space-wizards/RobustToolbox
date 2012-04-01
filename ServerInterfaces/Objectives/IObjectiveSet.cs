namespace ServerInterfaces.Objectives
{
    public interface IObjectiveSet
    {
        IObjective[] GetObjectives();
        void AddObjective(IObjective objective);
        void RemoveObjective(IObjective objective);
    }
}
