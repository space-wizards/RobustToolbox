/*
using OpenTK;
using OpenTK.Graphics;
using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using SS14.Client.Graphics.Lighting;
using SS14.Client.Interfaces.Resource;
using YamlDotNet.RepresentationModel;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Shared.Map;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        public ILight Light { get; private set; }

        public Color4 Color
        {
            get => Light.Color;
            set => Light.Color = value;
        }

        private Vector2 offset = Vector2.Zero;
        public Vector2 Offset
        {
            get => offset;
            set
            {
                offset = value;
                UpdateLightPosition();
            }
        }

        public int Radius
        {
            get => Light.Radius;
            set => Light.Radius = value;
        }

        private string mask;
        protected string Mask
        {
            get => mask;
            set
            {
                mask = value;

                var sprMask = IoCManager.Resolve<IResourceCache>().GetSprite(value);
                Light.SetMask(sprMask);
            }
        }

        public LightModeClass ModeClass
        {
            get => Light.LightMode.LightModeClass;
            set => IoCManager.Resolve<ILightManager>().SetLightMode(value, Light);
        }

        public LightMode Mode => Light.LightMode;

        public LightState State
        {
            get => Light.LightState;
            set => Light.LightState = value;
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.GetComponent<ITransformComponent>().OnMove += OnMove;
            UpdateLightPosition();
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            Light = mgr.CreateLight();
            mgr.AddLight(Light);

            YamlNode node;
            if (mapping.TryGetNode("offset", out node))
            {
                Offset = node.AsVector2();
            }

            if (mapping.TryGetNode("radius", out node))
            {
                Radius = node.AsInt();
            }
            else
            {
                Radius = 512;
            }

            if (mapping.TryGetNode("color", out node))
            {
                Color = node.AsHexColor();
            }
            else
            {
                Color = new Color4(200, 200, 200, 255);
            }

            if (mapping.TryGetNode("mask", out node))
            {
                Mask = node.AsString();
            }
            else
            {
                Mask = "whitemask";
            }

            if (mapping.TryGetNode("state", out node))
            {
                State = node.AsEnum<LightState>();
            }
            else
            {
                State = LightState.On;
            }

            if (mapping.TryGetNode("mode", out node))
            {
                ModeClass = node.AsEnum<LightModeClass>();
            }
            else
            {
                ModeClass = LightModeClass.Constant;
            }
        }

        public override void Shutdown()
        {
            Owner.GetComponent<ITransformComponent>().OnMove -= OnMove;
            IoCManager.Resolve<ILightManager>().RemoveLight(Light);
            base.Shutdown();
        }

        private void OnMove(object sender, MoveEventArgs args)
        {
            UpdateLightPosition(args.NewPosition);
        }

        protected void UpdateLightPosition(LocalCoordinates NewPosition)
        {
            Light.Coordinates = new LocalCoordinates(NewPosition.Position + Offset, NewPosition.Grid);
        }

        protected void UpdateLightPosition()
        {
            var transform = Owner.GetComponent<ITransformComponent>();
            UpdateLightPosition(transform.LocalPosition);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            Light.Update(frameTime);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (PointLightComponentState)state;
            State = newState.State;
            Color = newState.Color;
            ModeClass = newState.Mode;
        }
    }
}
*/
