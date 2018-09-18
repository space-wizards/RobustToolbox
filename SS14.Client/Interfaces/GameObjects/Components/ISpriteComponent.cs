using System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Shaders;
using SS14.Client.UserInterface;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Interfaces.GameObjects.Components
{
    public interface ISpriteComponent : IComponent
    {
        void FrameUpdate(float delta);

        bool Visible { get; set; }

        /// <summary>
        ///     Z-index for drawing.
        /// </summary>
        DrawDepth DrawDepth { get; set; }

        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        Vector2 Scale { get; set; }

        /// <summary>
        ///     A rotation applied to all layers.
        /// </summary>
        Angle Rotation { get; set; }

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

        // NOTE: The below are ALL designed to NOT throw exceptions ever,
        // instead making a bunch of noisy error logs.

        /// <summary>
        ///     The RSI that is currently used as "base".
        ///     Layers will fall back to this RSI if they do not have their own RSI set.
        /// </summary>
        RSI BaseRSI { get; set; }

        /// <summary>
        ///     Sets a layer key to the layer map, creating it if it does not exist.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <param name="layer">The layer this entry points to.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="layer"/> does not exist.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="key"/> is null.
        /// </exception>
        void LayerMapSet(object key, int layer);

        /// <summary>
        ///     Removes an entry from the layer map.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="key"/> is null.
        /// </exception>
        void LayerMapRemove(object key);

        /// <summary>
        ///     Gets the index of a layer specified in the layer map.
        /// </summary>
        /// <param name="key">The key for the entry to look up.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="key"/> is null.
        /// </exception>
        int LayerMapGet(object key);

        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="key"/> is null.
        /// </exception>
        bool LayerMapTryGet(object key, out int layer);

        /// <summary>
        ///     Adds a layer without texture (thus falling back to the error texture).
        ///     The layer defaults to invisible.
        /// </summary>
        /// <param name="newIndex">If not null, the index of this new layer.</param>
        /// <returns>Index of the new layer.</returns>
        int AddBlankLayer(int? newIndex = null);

        int AddLayer(Texture texture, int? newIndex = null);
        int AddLayer(string texturePath, int? newIndex = null);
        int AddLayer(ResourcePath texturePath, int? newIndex = null);
        int AddLayer(RSI.StateId stateId, int? newIndex = null);
        int AddLayerState(string stateId, int? newIndex = null);
        int AddLayer(RSI.StateId stateId, RSI rsi, int? newIndex = null);
        int AddLayerState(string stateId, RSI rsi, int? newIndex = null);
        int AddLayer(RSI.StateId stateId, string rsiPath, int? newIndex = null);
        int AddLayerState(string stateId, string rsiPath, int? newIndex = null);
        int AddLayer(RSI.StateId stateId, ResourcePath rsiPath, int? newIndex = null);
        int AddLayerState(string stateId, ResourcePath rsiPath, int? newIndex = null);
        int AddLayer(SpriteSpecifier specifier, int? newIndex = null);

        void RemoveLayer(int layer);
        void RemoveLayer(object layerKey);

        void LayerSetShader(int layer, Shader shader);
        void LayerSetShader(object layerKey, Shader shader);
        void LayerSetShader(int layer, string shaderName);
        void LayerSetShader(object layerKey, string shaderName);

        void LayerSetSprite(int layer, SpriteSpecifier specifier);
        void LayerSetSprite(object layerKey, SpriteSpecifier specifier);

        void LayerSetTexture(int layer, Texture texture);
        void LayerSetTexture(object layerKey, Texture texture);
        void LayerSetTexture(int layer, string texturePath);
        void LayerSetTexture(object layerKey, string texturePath);
        void LayerSetTexture(int layer, ResourcePath texturePath);
        void LayerSetTexture(object layerKey, ResourcePath texturePath);

        void LayerSetState(int layer, RSI.StateId stateId);
        void LayerSetState(object layerKey, RSI.StateId stateId);
        void LayerSetState(int layer, RSI.StateId stateId, RSI rsi);
        void LayerSetState(object layerKey, RSI.StateId stateId, RSI rsi);
        void LayerSetState(int layer, RSI.StateId stateId, string rsiPath);
        void LayerSetState(object layerKey, RSI.StateId stateId, string rsiPath);
        void LayerSetState(int layer, RSI.StateId stateId, ResourcePath rsiPath);
        void LayerSetState(object layerKey, RSI.StateId stateId, ResourcePath rsiPath);

        void LayerSetRSI(int layer, RSI rsi);
        void LayerSetRSI(object layerKey, RSI rsi);
        void LayerSetRSI(int layer, string rsiPath);
        void LayerSetRSI(object layerKey, string rsiPath);
        void LayerSetRSI(int layer, ResourcePath rsiPath);
        void LayerSetRSI(object layerKey, ResourcePath rsiPath);

        void LayerSetScale(int layer, Vector2 scale);
        void LayerSetScale(object layerKey, Vector2 scale);
        void LayerSetRotation(int layer, Angle rotation);
        void LayerSetRotation(object layerKey, Angle rotation);
        void LayerSetVisible(int layer, bool visible);
        void LayerSetVisible(object layerKey, bool visible);
        void LayerSetColor(int layer, Color color);
        void LayerSetColor(object layerKey, Color color);
        // Yes, I realize how silly it is to reference an enum in the concrete implementation.
        // I don't care.
        void LayerSetDirOffset(int layer, SpriteComponent.DirectionOffset offset);
        void LayerSetDirOffset(object layerKey, SpriteComponent.DirectionOffset offset);

        ISpriteProxy CreateProxy();
    }

    /// <summary>
    ///     A handle to allow a <see cref="ISpriteComponent"/> to draw in multiple locations, such as a UI slot.
    /// </summary>
    public interface ISpriteProxy : IDisposable
    {
        Vector2 Offset { get; set; }
        void AttachToItem(Godot.RID item);
    }

    public static class SpriteProxyExt
    {
        public static void AttachToControl(this ISpriteProxy mirror, Control control)
        {
            mirror.AttachToItem(control.SceneControl.GetCanvasItem());
        }
    }
}
