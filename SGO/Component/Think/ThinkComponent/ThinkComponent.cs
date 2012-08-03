namespace SGO.Component.Think.ThinkComponent
{
    public class ThinkComponent : IThinkComponent
    {
        #region IThinkComponent Members

        public virtual void OnBump(object sender, params object[] list)
        {
            //list[0] is the bumping ent
        }

        public virtual void Update(float frameTime)
        {
        }

        #endregion
    }
}