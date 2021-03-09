using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.EntityLookup;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class EntityLookupSystem : SharedEntityLookupSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        private VBoxContainer? _control;

        private HashSet<IEntity> _nodeEntities = new();

        public bool DebugNodes
        {
            get => _debugNodes;
            set
            {
                if (_debugNodes == value)
                    return;

                _debugNodes = value;

                if (_debugNodes)
                {
                    _control = new VBoxContainer();
                    IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(_control);
                }
                else
                {
                    _control?.Dispose();
                    _control = null;
                }
            }
        }

        private bool _debugNodes;

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            if (_control == null) return;
            _control.Visible = true;

            var mousePos = _inputManager.MouseScreenPosition;
            var mapPos = _eyeManager.ScreenToMap(mousePos);

            if (!TryGetNode(mapPos.MapId, mapPos.Position, out var node))
            {
                _control.DisposeAllChildren();
                return;
            }

            _nodeEntities.Clear();
            _nodeEntities.UnionWith(node.Entities);

            foreach (var entity in _nodeEntities.ToArray())
            {
                foreach (var con in entity.GetContained())
                {
                    _nodeEntities.Add(con);
                }
            }

            _control.DisposeAllChildren();

            foreach (var entity in _nodeEntities)
            {
                _control.AddChild(new Label
                {
                    Text = $"uid: {entity.Uid}, name: {entity.Name}"
                });
            }

            LayoutContainer.SetPosition(_control, mousePos + new Vector2(0, 20));
        }
    }
}
