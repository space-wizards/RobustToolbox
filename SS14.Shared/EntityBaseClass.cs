namespace SS14.Shared
{
    public class EntityBaseClass //This class is the base class for all other objects in the game. 
    //Think of it as byonds ATOM. Add shared variables here.
    //This should always be in the entities UserObject. So make sure to set it.
    {
        public string Name { get; set; }
        public int Uid { get; set; }
    }
}