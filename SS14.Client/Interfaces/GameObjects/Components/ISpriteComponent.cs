using SS14.Client.Graphics;
using SS14.Client.Graphics.Shaders;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GameObjects.Components
{
    public interface ISpriteComponent : IComponent
    {
        void FrameUpdate(float delta);

        /// <summary>
        ///     Z-index for drawing.
        /// </summary>
        DrawDepth DrawDepth { get; set; }

        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        Vector2 Scale { get; set; }

        /// <summary>
        ///     Offset applied to all layers.
        /// </summary>
        Vector2 Offset { get; set; }

        /// <summary>
        ///     Color to multiply all layers with.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        ///     Controls whether we use RSI directions to rotate, or just get angular rotation applied.
        ///     If true, all rotation to this sprite component is negated (that is rotation from say the owner being rotated).
        ///     Rotation transformations on individual layers still apply.
        ///     If false, all layers get locked to south and rotation is a transformation.
        /// </summary>
        bool Directional { get; set; }

        /// <summary>
        ///     The RSI that is currently used as "base".
        ///     Layers will fall back to this RSI if they do not have their own RSI set.
        /// </summary>
        RSI BaseRSI { get; set; }

        int AddLayer(Texture texture);
        int AddLayer(RSI.StateId stateId);
        int AddLayer(RSI.StateId stateId, RSI rsi);
        void RemoveLayer(int layer);

        void LayerSetShader(int layer, Shader shader);
        void LayerSetTexture(int layer, Texture texture);
        void LayerSetState(int layer, RSI.StateId stateId, RSI rsi = null);
        void LayerSetRSI(int layer, RSI rsi);
    }
}
