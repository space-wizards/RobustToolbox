using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Services.Helpers;
using SS14.Shared;
using SS14.Shared.GameObjects;


namespace SS14.Client.Services.UserInterface
{
    public class DragDropInfo : IDragDropInfo
    {
        #region IDragDropInfo Members

        public Entity DragEntity { get; private set; }
        public Sprite DragSprite { get; private set; }
        public IPlayerAction DragAction { get; private set; }
        public bool IsEntity { get; private set; }

        public bool IsActive
        {
            get { return Active(); }
        }

        public void Reset()
        {
            DragEntity = null;
            DragSprite = null;
            DragAction = null;
            IsEntity = true;
        }

        public void StartDrag(Entity entity)
        {
            Reset();

            IoCManager.Resolve<IUserInterfaceManager>().CancelTargeting();
            IoCManager.Resolve<IPlacementManager>().Clear();

            DragEntity = entity;
            DragSprite = Utilities.GetIconSprite(entity);
            IsEntity = true;
        }

        public void StartDrag(IPlayerAction action)
        {
            Reset();
            DragAction = action;
            DragSprite = IoCManager.Resolve<IResourceManager>().GetSprite(action.Icon);
            IsEntity = false;
        }

        #endregion

        public bool Active()
        {
            return (DragAction != null || DragEntity != null);
        }
    }
}