using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;

namespace SS14.Client.GameObjects
{
    public class EffectSystem : EntitySystem
    {
        private List<Effect> _Effects = new List<Effect>();
        private TimeSpan lasttimeprocessed = TimeSpan.Zero;

        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<EffectSystemMessage>();
        }

        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            switch (message)
            {
                case EffectSystemMessage msg:
                    CreateEffect(msg);
                    break;
            }
        }

        public void CreateEffect(EffectSystemMessage message)
        {
            var gametime = IoCManager.Resolve<IGameTiming>().CurTime;
            if (gametime > message.DeathTime) //Did we already die in transit? That's pretty troubling isn't it
            {
                Logger.Warning(string.Format("Effect using sprite {0} died in transit to the client", message.EffectSprite), message);
                return;
            }

            //Create effect from creation message
            var effect = new Effect(message);


            //Remove this
            effect.Age = gametime;
            effect.Deathtime = effect.Age + TimeSpan.FromSeconds(4);

            //Age the effect through a single update to the previous update tick of the effect system
            //effect.Update((float)((lasttimeprocessed - effect.Age).TotalSeconds));

            _Effects.Add(effect);
        }

        public override void Update(float frameTime)
        {
            lasttimeprocessed = IoCManager.Resolve<IGameTiming>().CurTime;

            for (int i = 0; i < _Effects.Count; i++)
            {
                var effect = _Effects[i];

                //Update variables of the effect via its deltas
                effect.Update(frameTime);

                //These effects have died
                if(effect.Age > effect.Deathtime)
                {
                    //Remove from the effects list and decrement the iterator
                    _Effects.Remove(effect);
                    i--;
                }
            }
        }

        public void Render(MapId map)
        {
            foreach (var effect in _Effects)
            {
                if(effect.Coordinates.MapID == map)
                {
                    Sprite effectsprite = effect.EffectSprite;
                    effectsprite.Color = ToColor(effect.Color);
                    effectsprite.Position = CluwneLib.WorldToScreen(effect.Coordinates.ToWorld().Position);
                    effectsprite.Rotation = effect.Rotation;
                    effectsprite.Scale = effect.Size;
                    //You will come in search of this line of code to fix a 'bug' some day, 
                    //but you'll have a better idea about your use case to fix it for by then
                    effectsprite.Origin = new Vector2(0f, effectsprite.TextureRect.Height / 2);
                    effectsprite.Draw();
                }
            }
        }


        private class Effect
        {
            /// <summary>
            /// Effect Sprite
            /// This is the sprite that will be drawn as the "effect".
            /// </summary>
            public Sprite EffectSprite { get; set; }

            /// <summary>
            /// Effect position relative to the emit position
            /// </summary>
            public LocalCoordinates Coordinates;

            /// <summary>
            /// Where the emitter was when the effect was first emitted
            /// </summary>
            public LocalCoordinates EmitterCoordinates;

            /// <summary>
            /// Effect's x/y velocity
            /// </summary>
            public Vector2 Velocity = new Vector2(0, 0);

            /// <summary>
            /// Effect's x/y acceleration
            /// </summary>
            public Vector2 Acceleration = new Vector2(0, 0);

            /// <summary>
            /// Effect's radial velocity - relative to EmitterPosition
            /// </summary>
            public float RadialVelocity = 0f;

            /// <summary>
            /// Effect's radial acceleration
            /// </summary>
            public float RadialAcceleration = 0f;

            /// <summary>
            /// Effect's tangential velocity - relative to EmitterPosition
            /// </summary>
            public float TangentialVelocity = 0f;

            /// <summary>
            /// Effect's tangential acceleration
            /// </summary>
            public float TangentialAcceleration = 0f;

            /// <summary>
            /// Effect's spin about its center in radians
            /// </summary>
            public float Rotation = 0f;

            /// <summary>
            /// Rate of change of effect's spin
            /// </summary>
            public float RotationRate = 0f;

            /// <summary>
            /// Effect's current size
            /// </summary>
            public Vector2 Size = new Vector2(1f, 1f);

            /// <summary>
            /// Rate of change of effect's size change
            /// </summary>
            public float SizeDelta = 0f;

            /// <summary>
            /// Effect's current color
            /// </summary>
            public Vector4 Color = new Vector4(255,255,255,255);

            /// <summary>
            /// Rate of change of effect's color
            /// </summary>
            public Vector4 ColorDelta = new Vector4(0, 0, 0, 0);

            /// <summary>
            /// Effect's age -- from 0f
            /// </summary>
            public TimeSpan Age = TimeSpan.Zero;

            /// <summary>
            /// Time after which the effect will "die"
            /// </summary>
            public TimeSpan Deathtime = TimeSpan.FromSeconds(1);
            

            public Effect(EffectSystemMessage effectcreation)
            {
                EffectSprite = new Sprite(IoCManager.Resolve<IResourceCache>().GetSprite(effectcreation.EffectSprite));
                Coordinates = effectcreation.Coordinates;
                EmitterCoordinates = effectcreation.EmitterCoordinates;
                Velocity = effectcreation.Velocity;
                Acceleration = effectcreation.Acceleration;
                RadialVelocity = effectcreation.RadialVelocity;
                RadialAcceleration = effectcreation.RadialAcceleration;
                TangentialVelocity = effectcreation.TangentialVelocity;
                TangentialAcceleration = effectcreation.TangentialAcceleration;
                Age = effectcreation.Born;
                Deathtime = effectcreation.DeathTime;
                Rotation = effectcreation.Rotation;
                RotationRate = effectcreation.RotationRate;
                Size = effectcreation.Size;
                SizeDelta = effectcreation.SizeDelta;
                Color = effectcreation.Color;
                ColorDelta = effectcreation.ColorDelta;
            }

            public void Update(float frameTime)
            {
                Age += TimeSpan.FromSeconds(frameTime);
                if (Age >= Deathtime)
                    return;

                Velocity += Acceleration*frameTime;
                RadialVelocity += RadialAcceleration*frameTime;
                TangentialVelocity += TangentialAcceleration*frameTime;
                
                var deltaPosition = new Vector2(0f, 0f);

                //If we have an emitter we can do special effects around that emitter position
                if(EmitterCoordinates != null)
                {
                    //Calculate delta p due to radial velocity
                    var positionRelativeToEmitter = Coordinates.ToWorld().Position - EmitterCoordinates.ToWorld().Position;
                    var deltaRadial = RadialVelocity * frameTime;
                    deltaPosition = positionRelativeToEmitter * (deltaRadial / positionRelativeToEmitter.Length);

                    //Calculate delta p due to tangential velocity
                    var radius = positionRelativeToEmitter.Length;
                    if (radius > 0)
                    {
                        var theta = (float)Math.Atan2(positionRelativeToEmitter.Y, positionRelativeToEmitter.X);
                        theta += TangentialVelocity * frameTime;
                        deltaPosition += new Vector2(radius * (float)Math.Cos(theta), radius * (float)Math.Sin(theta))
                                         - positionRelativeToEmitter;
                    }
                }

                //Calculate new position from our velocity as well as possible rotation/movement around emitter
                deltaPosition += Velocity*frameTime;
                Coordinates = new LocalCoordinates(Coordinates.Position + deltaPosition, Coordinates.Grid);

                //Finish calculating new rotation, size, color
                Rotation += RotationRate*frameTime;
                Size += SizeDelta*frameTime;
                Color += ColorDelta*frameTime;
            }
        }

        private Color ToColor(Vector4 color)
        {
            color = Limit(color);

            return new Color((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W);
        }

        private Vector4 Limit(Vector4 color)
        {
            return new Vector4(FloatMath.Clamp(color.X, 0f, 255f), FloatMath.Clamp(color.Y, 0f, 255f), FloatMath.Clamp(color.Z, 0f, 255f), FloatMath.Clamp(color.W, 0f, 255f));
        }
    }
}
