using SFML.Graphics;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Helpers;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Client.UserInterface
{
    public class DragDropInfo : IDragDropInfo
    {
        #region IDragDropInfo Members

        public IEntity DragEntity { get; private set; }
        public Sprite DragSprite { get; private set; }
        public bool IsEntity { get; private set; }

        public bool IsActive
        {
            get { return Active(); }
        }

        public void Reset()
        {
            DragEntity = null;
            DragSprite = null;
            IsEntity = true;
        }

        public void StartDrag(IEntity entity)
        {
            Reset();

            IoCManager.Resolve<IPlacementManager>().Clear();

            DragEntity = entity;
            DragSprite = Utilities.GetIconSprite(entity);
            IsEntity = true;
        }

        #endregion IDragDropInfo Members

        public bool Active()
        {
            return DragEntity != null;
        }
    }
}
