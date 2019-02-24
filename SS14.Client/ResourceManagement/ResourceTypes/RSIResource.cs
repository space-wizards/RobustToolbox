using Newtonsoft.Json.Linq;
using NJsonSchema;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SS14.Client.ResourceManagement
{
    /// <summary>
    ///     Handles the loading code for RSI files.
    ///     See <see cref="RSI"/> for the RSI API itself.
    /// </summary>
    public sealed class RSIResource : BaseResource
    {
        public RSI RSI { get; private set; }

        /// <summary>
        ///     The minimum version of RSI we can load.
        /// </summary>
        public const uint MINIMUM_RSI_VERSION = 1;

        /// <summary>
        ///     The maximum version of RSI we can load.
        /// </summary>
        public const uint MAXIMUM_RSI_VERSION = 1;

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            var manifestPath = path / "meta.json";
            string manifestContents;

            using (var manifestFile = cache.ContentFileRead(manifestPath))
            using (var reader = new StreamReader(manifestFile))
            {
                manifestContents = reader.ReadToEnd();
            }

            if (RSISchema != null)
            {
                var errors = RSISchema.Validate(manifestContents);
                if (errors.Count != 0)
                {
                    Logger.Error($"Unable to load RSI from '{path}', {errors.Count} errors:");

                    foreach (var error in errors)
                    {
                        Logger.Error("{0}", error.ToString());
                    }

                    throw new RSILoadException($"{errors.Count} errors while loading RSI. See console.");
                }
            }

            // Ok schema validated just fine.
            var manifestJson = JObject.Parse(manifestContents);
            var size = manifestJson["size"].ToObject<Vector2u>();

            var rsi = new RSI(size);

            // Do every state.
            foreach (var stateObject in manifestJson["states"].Cast<JObject>())
            {
                var stateName = stateObject["name"].ToObject<string>();
                var dirValue = stateObject["directions"].ToObject<int>();
                RSI.State.DirectionType directions;

                switch (dirValue)
                {
                    case 1:
                        directions = RSI.State.DirectionType.Dir1;
                        break;
                    case 4:
                        directions = RSI.State.DirectionType.Dir4;
                        break;
                    default:
                        throw new RSILoadException($"Invalid direction: {dirValue}");
                }

                // We can ignore selectors and flags for now,
                // because they're not used yet!

                // Get the lists of delays.
                float[][] delays;
                if (stateObject.TryGetValue("delays", out var delayToken))
                {
                    delays = delayToken.ToObject<float[][]>();

                    if (delays.Length != dirValue)
                    {
                        throw new RSILoadException($"Directions count does not match amount of delays specified.");
                    }

                    for (var i = 0; i < delays.Length; i++)
                    {
                        var delayList = delays[i];
                        if (delayList.Length == 0)
                        {
                            delays[i] = new float[] {1};
                        }
                    }
                }
                else
                {
                    delays = new float[dirValue][];
                    // No delays specified, default to 1 frame per dir.
                    for (var i = 0; i < dirValue; i++)
                    {
                        delays[i] = new float[] {1};
                    }
                }

                var texPath = path / (stateName + ".png");
                var texture = cache.GetResource<TextureResource>(texPath).Texture;

                if (texture.Width % size.X != 0 || texture.Height % size.Y != 0)
                {
                    throw new RSILoadException("State image size is not a multiple of the icon size.");
                }

                // Amount of icons per row of the sprite sheet.
                var sheetWidth = texture.Width / size.X;

                var iconFrames = new (Texture, float)[dirValue][];
                var counter = 0;
                for (var j = 0; j < iconFrames.Length; j++)
                {
                    var delayList = delays[j];
                    var directionFrames = new (Texture, float)[delayList.Length];
                    for (var i = 0; i < delayList.Length; i++)
                    {
                        if (!GameController.OnGodot)
                        {
                            directionFrames[i] = (new BlankTexture(), delayList[i]);
                            continue;
                        }
                        var PosX = (counter % sheetWidth) * size.X;
                        var PosY = (counter / sheetWidth) * size.Y;

                        var atlasTexture = new Godot.AtlasTexture()
                        {
                            Atlas = texture,
                            Region = new Godot.Rect2(PosX, PosY, size.X, size.Y)
                        };

                        directionFrames[i] = (new GodotTextureSource(atlasTexture), delayList[i]);
                        counter++;
                    }

                    iconFrames[j] = directionFrames;
                }

                var state = new RSI.State(size, stateName, directions, iconFrames);
                rsi.AddState(state);
            }

            RSI = rsi;
        }

        private static readonly JsonSchema4 RSISchema = GetSchema();

        private static JsonSchema4 GetSchema()
        {
            try
            {
                string schema;
                using (var schemaStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("SS14.Client.Graphics.RSI.RSISchema.json"))
                using (var schemaReader = new StreamReader(schemaStream))
                {
                    schema = schemaReader.ReadToEnd();
                }

                return JsonSchema4.FromJsonAsync(schema).Result;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Failed to load RSI JSON Schema!\n{0}", e);
                return null;
            }
        }
    }

    [Serializable]
    public class RSILoadException : Exception
    {
        public RSILoadException()
        {
        }

        public RSILoadException(string message) : base(message)
        {
        }

        public RSILoadException(string message, Exception inner) : base(message, inner)
        {
        }

        protected RSILoadException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
