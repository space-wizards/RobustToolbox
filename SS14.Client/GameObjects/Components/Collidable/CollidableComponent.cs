using System;
using OpenTK;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Utility;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        // no client side collision support for now
        private bool collisionEnabled;

        public Color DebugColor { get; private set; } = Color.Red;

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public override Type StateType => typeof(CollidableComponentState);

        /// <inheritdoc />
        Box2 ICollidable.WorldAABB => Owner.GetComponent<BoundingBoxComponent>().WorldAABB;

        /// <inheritdoc />
        Box2 ICollidable.AABB => Owner.GetComponent<BoundingBoxComponent>().AABB;

        /// <inheritdoc />
        public int MapID => Owner.GetComponent<ITransformComponent>().MapID;

        private bool _debugDraw = false;
        public bool DebugDraw
        {
            get => _debugDraw;
            set
            {
                if (value == _debugDraw)
                {
                    return;
                }

                _debugDraw = value;
                debugNode.Update();
            }
        }

        private Godot.Node2D debugNode;
        private IClientTransformComponent transform;
        private GodotGlue.GodotSignalSubscriber0 debugDrawSubscriber;

        /// <summary>
        ///     Called when the collidable is bumped into by someone/something
        /// </summary>
        void ICollidable.Bump(IEntity ent)
        {
            OnBump?.Invoke(this, new BumpEventArgs(this.Owner, ent));

            Owner.SendMessage(this, ComponentMessageType.Bumped, ent);
        }

        /// <inheritdoc />
        public bool IsHardCollidable { get; } = true;

        /// <summary>
        ///     gets the AABB from the sprite component and sends it to the CollisionManager.
        /// </summary>
        /// <param name="owner"></param>
        public override void Initialize()
        {
            base.Initialize();

            if (collisionEnabled)
            {
                var cm = IoCManager.Resolve<ICollisionManager>();
                cm.AddCollidable(this);
            }

            transform = Owner.GetComponent<IClientTransformComponent>();
            debugNode = new Godot.Node2D();
            debugNode.SetName("Collidable debug");
            debugDrawSubscriber = new GodotGlue.GodotSignalSubscriber0();
            debugDrawSubscriber.Connect(debugNode, "draw");
            debugDrawSubscriber.Signal += DrawDebugRect;
            transform.SceneNode.AddChild(debugNode);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            debugDrawSubscriber.Disconnect(debugNode, "draw");
            debugDrawSubscriber.Dispose();
            debugDrawSubscriber = null;

            debugNode.QueueFree();
            debugNode.Dispose();
            debugNode = null;
        }

        /// <summary>
        ///     removes the AABB from the CollisionManager.
        /// </summary>
        public override void Shutdown()
        {
            if (collisionEnabled)
            {
                var cm = IoCManager.Resolve<ICollisionManager>();
                cm.RemoveCollidable(this);
            }

            base.Shutdown();
        }

        /// <summary>
        ///     SpriteChanged means the spritecomponent changed the current sprite.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="reply"></param>
        /// <param name="list"></param>
        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
            params object[] list)
        {
            var reply = base.ReceiveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SpriteChanged:
                    if (collisionEnabled)
                    {
                        var cm = IoCManager.Resolve<ICollisionManager>();
                        cm.UpdateCollidable(this);
                    }
                    break;
            }

            return reply;
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (CollidableComponentState)state;

            // edge triggered
            if (newState.CollisionEnabled == collisionEnabled)
                return;

            if (newState.CollisionEnabled)
                EnableCollision();
            else
                DisableCollision();
        }

        public bool TryCollision(Vector2 offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }

        public event EventHandler<BumpEventArgs> OnBump;

        /// <summary>
        ///     Enables collidable
        /// </summary>
        private void EnableCollision()
        {
            collisionEnabled = true;
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.AddCollidable(this);
        }

        /// <summary>
        ///     Disables Collidable
        /// </summary>
        private void DisableCollision()
        {
            collisionEnabled = false;
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.RemoveCollidable(this);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            if (mapping.TryGetNode("DebugColor", out var node))
            {
                DebugColor = node.AsHexColor();
            }
        }

        private void DrawDebugRect()
        {
            if (!DebugDraw)
            {
                return;
            }
            var colorEdge = DebugColor.WithAlpha(0.50f).Convert();
            var colorFill = DebugColor.WithAlpha(0.25f).Convert();
            var aabb = Owner.GetComponent<BoundingBoxComponent>().AABB;

            var rect = new Godot.Rect2(aabb.Left * 32, aabb.Top * 32, aabb.Width * 32, aabb.Height * 32);
            debugNode.DrawRect(rect, colorEdge, filled: false);
            rect.Position += new Godot.Vector2(1, 1);
            rect.Size -= new Godot.Vector2(2, 2);
            debugNode.DrawRect(rect, colorFill, filled: true);
        }
    }
}
