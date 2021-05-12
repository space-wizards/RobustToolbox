using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using System;
using System.Collections.Generic;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Enums;

namespace Robust.Client.GameObjects
{
    public class EffectSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming gameTiming = default!;
        [Dependency] private readonly IResourceCache resourceCache = default!;
        [Dependency] private readonly IEyeManager eyeManager = default!;
        [Dependency] private readonly IOverlayManager overlayManager = default!;
        [Dependency] private readonly IPrototypeManager prototypeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private readonly List<Effect> _Effects = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<EffectSystemMessage>(CreateEffect);
            SubscribeLocalEvent<EffectSystemMessage>(CreateEffect);

            var overlay = new EffectOverlay(this, prototypeManager, _mapManager, _playerManager, _entityManager);
            overlayManager.AddOverlay(overlay);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            overlayManager.RemoveOverlay(typeof(EffectOverlay));
        }

        public void CreateEffect(EffectSystemMessage message)
        {
            // The source of effects is either local actions during FirstTimePredicted, or the network at LastServerTick
            // When replaying predicted input, don't spam effects.
            if(gameTiming.InPrediction && !gameTiming.IsFirstTimePredicted)
                return;

            if (message.AttachedEntityUid != null && message.Coordinates != default)
            {
                Logger.Warning("Set both an AttachedEntityUid and EntityCoordinates on an EffectSystemMessage for sprite {0} which is not supported!", message.EffectSprite);
            }

            if (message.LifeTime <= TimeSpan.Zero)
            {
                Logger.Warning("Effect using sprite {0} had zero lifetime.", message.EffectSprite);
                return;
            }

            //Create effect from creation message
            var effect = new Effect(message, resourceCache, _mapManager, _entityManager);
            effect.Deathtime = gameTiming.CurTime + message.LifeTime;
            if (effect.AttachedEntityUid != null
                && _entityManager.TryGetEntity(effect.AttachedEntityUid.Value, out var attachedEntity))
            {
                effect.AttachedEntity = attachedEntity;
            }

            _Effects.Add(effect);
        }

        public override void FrameUpdate(float frameTime)
        {
            var curTime = gameTiming.CurTime;
            for (int i = 0; i < _Effects.Count; i++)
            {
                var effect = _Effects[i];

                //These effects have died
                // Effects are purely visual, so they don't need to be ran through prediction.
                // once CurTime ever passes DeathTime (clients render the top at IsFirstTimePredicted, where this happens) just remove them.
                if (curTime > effect.Deathtime)
                {
                    //Remove from the effects list and decrement the iterator
                    _Effects.Remove(effect);
                    i--;
                }
                else
                {
                    //Update variables of the effect via its deltas
                    effect.Update(frameTime);
                }
            }
        }

        private class Effect
        {
            /// <summary>
            /// Effect Sprite
            /// This is the sprite that will be drawn as the "effect".
            /// </summary>
            public Texture EffectSprite { get; set; }

            public RSI.State? RsiState { get; set; }

            public int AnimationIndex { get; set; }

            public float AnimationTime { get; set; }

            public bool AnimationLoops { get; set; }

            /// <summary>
            /// Entity that the effect is attached to
            /// </summary>
            public IEntity? AttachedEntity { get; set; }

            public EntityUid? AttachedEntityUid { get; }

            /// <summary>
            /// Offset relative to the attached entity
            /// </summary>
            public Vector2 AttachedOffset { get; }

            /// <summary>
            /// Effect position relative to the emit position
            /// </summary>
            public EntityCoordinates Coordinates;

            /// <summary>
            /// Where the emitter was when the effect was first emitted
            /// </summary>
            public EntityCoordinates EmitterCoordinates;

            /// <summary>
            /// Effect's x/y velocity
            /// </summary>
            public Vector2 Velocity = Vector2.Zero;

            /// <summary>
            /// Effect's x/y acceleration
            /// </summary>
            public Vector2 Acceleration = Vector2.Zero;

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
            public Vector2 Size = new(1f, 1f);

            /// <summary>
            /// Rate of change of effect's size change
            /// </summary>
            public float SizeDelta = 0f;

            /// <summary>
            /// Effect's current color
            /// </summary>
            public Vector4 Color = new(255, 255, 255, 255);

            /// <summary>
            /// Rate of change of effect's color
            /// </summary>
            public Vector4 ColorDelta = new(0, 0, 0, 0);

            /// <summary>
            ///     True if the effect is affected by lighting.
            /// </summary>
            public bool Shaded = true;

            /// <summary>
            /// CurTime after which the effect will "die"
            /// </summary>
            public TimeSpan Deathtime;

            private readonly IMapManager _mapManager;
            private readonly IEntityManager _entityManager;

            public Effect(EffectSystemMessage effectcreation, IResourceCache resourceCache, IMapManager mapManager, IEntityManager entityManager)
            {
                if (effectcreation.RsiState != null)
                {
                    var rsi = resourceCache
                        .GetResource<RSIResource>(new ResourcePath("/Textures/") / effectcreation.EffectSprite)
                        .RSI;
                    RsiState = rsi[effectcreation.RsiState];
                    EffectSprite = RsiState.Frame0;
                }
                else
                {
                    EffectSprite = resourceCache
                        .GetResource<TextureResource>(new ResourcePath("/Textures/") / effectcreation.EffectSprite)
                        .Texture;
                }

                AnimationLoops = effectcreation.AnimationLoops;
                AttachedEntityUid = effectcreation.AttachedEntityUid;
                AttachedOffset = effectcreation.AttachedOffset;
                Coordinates = effectcreation.Coordinates;
                EmitterCoordinates = effectcreation.EmitterCoordinates;
                Velocity = effectcreation.Velocity;
                Acceleration = effectcreation.Acceleration;
                RadialVelocity = effectcreation.RadialVelocity;
                RadialAcceleration = effectcreation.RadialAcceleration;
                TangentialVelocity = effectcreation.TangentialVelocity;
                TangentialAcceleration = effectcreation.TangentialAcceleration;
                Rotation = effectcreation.Rotation;
                RotationRate = effectcreation.RotationRate;
                Size = effectcreation.Size;
                SizeDelta = effectcreation.SizeDelta;
                Color = effectcreation.Color;
                ColorDelta = effectcreation.ColorDelta;
                Shaded = effectcreation.Shaded;
                _mapManager = mapManager;
                _entityManager = entityManager;
            }

            public void Update(float frameTime)
            {
                Velocity += Acceleration * frameTime;
                RadialVelocity += RadialAcceleration * frameTime;
                TangentialVelocity += TangentialAcceleration * frameTime;

                var deltaPosition = new Vector2(0f, 0f);

                //If we have an emitter we can do special effects around that emitter position
                if (_mapManager.GridExists(EmitterCoordinates.GetGridId(_entityManager)))
                {
                    //Calculate delta p due to radial velocity
                    var positionRelativeToEmitter =
                        Coordinates.ToMapPos(_entityManager) - EmitterCoordinates.ToMapPos(_entityManager);
                    var deltaRadial = RadialVelocity * frameTime;
                    deltaPosition = positionRelativeToEmitter * (deltaRadial / positionRelativeToEmitter.Length);

                    //Calculate delta p due to tangential velocity
                    var radius = positionRelativeToEmitter.Length;
                    if (radius > 0)
                    {
                        var theta = (float) Math.Atan2(positionRelativeToEmitter.Y, positionRelativeToEmitter.X);
                        theta += TangentialVelocity * frameTime;
                        deltaPosition += new Vector2(radius * (float) Math.Cos(theta), radius * (float) Math.Sin(theta))
                                         - positionRelativeToEmitter;
                    }
                }

                //Calculate new position from our velocity as well as possible rotation/movement around emitter
                deltaPosition += Velocity * frameTime;
                Coordinates = Coordinates.Offset(deltaPosition);

                //Finish calculating new rotation, size, color
                Rotation += RotationRate * frameTime;
                Size += SizeDelta * frameTime;
                Color += ColorDelta * frameTime;

                if (RsiState == null)
                {
                    return;
                }

                // Calculate RSI animations.
                var delayCount = RsiState.DelayCount;
                if (delayCount > 0 && (AnimationLoops || AnimationIndex < delayCount - 1))
                {
                    AnimationTime += frameTime;
                    while (RsiState.GetDelay(AnimationIndex) < AnimationTime)
                    {
                        var delay = RsiState.GetDelay(AnimationIndex);
                        AnimationIndex += 1;
                        AnimationTime -= delay;
                        if (AnimationIndex == delayCount)
                        {
                            if (AnimationLoops)
                            {
                                AnimationIndex = 0;
                            }
                            else
                            {
                                break;
                            }
                        }

                        EffectSprite = RsiState.GetFrame(RSI.State.Direction.South, AnimationIndex);
                    }
                }
            }
        }

        private static Color ToColor(Vector4 color)
        {
            color = Vector4.Clamp(color / 255f, Vector4.Zero, Vector4.One);

            return new Color(color.X, color.Y, color.Z, color.W);
        }

        private sealed class EffectOverlay : Overlay
        {
            private readonly IPlayerManager _playerManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            private readonly ShaderInstance _unshadedShader;
            private readonly EffectSystem _owner;
            private readonly IMapManager _mapManager;
            private readonly IEntityManager _entityManager;

            public EffectOverlay(EffectSystem owner, IPrototypeManager protoMan, IMapManager mapMan, IPlayerManager playerMan, IEntityManager entityManager)
            {
                _owner = owner;
                _unshadedShader = protoMan.Index<ShaderPrototype>("unshaded").Instance();
                _mapManager = mapMan;
                _playerManager = playerMan;
                _entityManager = entityManager;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                var map = _owner.eyeManager.CurrentMap;

                var worldHandle = args.WorldHandle;
                ShaderInstance? currentShader = null;
                var player = _playerManager.LocalPlayer?.ControlledEntity;

                foreach (var effect in _owner._Effects)
                {
                    if (effect.AttachedEntity?.Transform.MapID != player?.Transform.MapID &&
                        effect.Coordinates.GetMapId(_entityManager) != map)
                    {
                        continue;
                    }

                    var newShader = effect.Shaded ? null : _unshadedShader;

                    if (newShader != currentShader)
                    {
                        worldHandle.UseShader(newShader);
                        currentShader = newShader;
                    }

                    var effectSprite = effect.EffectSprite;
                    var effectOrigin = effect.AttachedEntity?.Transform.MapPosition.Position + effect.AttachedOffset ??
                                               effect.Coordinates.ToMapPos(_entityManager);

                    var effectArea = Box2.CenteredAround(effectOrigin, effect.Size);

                    var rotatedBox = new Box2Rotated(effectArea, effect.Rotation, effectOrigin);

                    worldHandle.DrawTextureRect(effectSprite, rotatedBox, ToColor(effect.Color));
                }
            }
        }
    }
}
