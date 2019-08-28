using Newtonsoft.Json.Linq;
using NJsonSchema;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Robust.Client.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.ResourceManagement
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

#if DEBUG
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
#endif

            // Ok schema validated just fine.
            var manifestJson = JObject.Parse(manifestContents);
            var size = manifestJson["size"].ToObject<Vector2u>();

            var rsi = new RSI(size);

            var images = new List<(Image<Rgba32> src, Vector2i offset)>();
            var directionFramesList = new List<(Texture, float)[]>();

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
                    case 8:
                        directions = RSI.State.DirectionType.Dir8;
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
                        throw new RSILoadException(
                            "DirectionsdirectionFramesList count does not match amount of delays specified.");
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
                var image = Image.Load(cache.ContentFileRead(texPath));
                var sheetSize = new Vector2i(image.Width, image.Height);

                if (sheetSize.X % size.X != 0 || sheetSize.Y % size.Y != 0)
                {
                    throw new RSILoadException("State image size is not a multiple of the icon size.");
                }

                // Amount of icons per row of the sprite sheet.
                var sheetWidth = (sheetSize.X / size.X);

                var iconFrames = new (Texture, float)[dirValue][];
                var counter = 0;
                for (var j = 0; j < iconFrames.Length; j++)
                {
                    var delayList = delays[j];
                    var directionFrames = new (Texture, float)[delayList.Length];
                    directionFramesList.Add(directionFrames);
                    for (var i = 0; i < delayList.Length; i++)
                    {
                        var posX = (int) ((counter % sheetWidth) * size.X);
                        var posY = (int) ((counter / sheetWidth) * size.Y);

                        images.Add((image, (posX, posY)));

                        directionFrames[i] = (null, delayList[i]);
                        counter++;
                    }

                    iconFrames[j] = directionFrames;
                }

                var state = new RSI.State(size, stateName, directions, iconFrames);
                rsi.AddState(state);
            }

            // Poorly hacked in texture atlas support here.
            {
                // Generate atlas.
                var dimensionX = (int) Math.Ceiling(Math.Sqrt(images.Count));
                var dimensionY = (int) Math.Ceiling((float) images.Count / dimensionX);

                int i;
                Texture texture;
                using (var sheet = new Image<Rgba32>((int) (dimensionX * size.X), (int) (dimensionY * size.Y)))
                {
                    i = 0;
                    foreach (var list in directionFramesList)
                    {
                        for (var j = 0; j < list.Length; j++, i++)
                        {
                            var column = i % dimensionX;
                            var row = i / dimensionX;

                            var (image, offset) = images[i];

                            var srcBox = UIBox2i.FromDimensions(offset, (Vector2i) size);
                            var dstOffset = ((int)(column * size.X), (int)(row * size.Y));
                            image.Blit(srcBox, sheet, dstOffset);
                        }
                    }

                    // Load atlas.
                    texture = Texture.LoadFromImage(sheet, path.ToString());
                }

                // Assign AtlasTexture instances.
                i = 0;
                foreach (var list in directionFramesList)
                {
                    for (var j = 0; j < list.Length; j++, i++)
                    {
                        ref var tuple = ref list[j];
                        var column = i % dimensionX;
                        var row = i / dimensionX;

                        var pX = (int) (column * size.X);
                        var pY = (int) (row * size.Y);

                        tuple.Item1 = new AtlasTexture(texture, UIBox2.FromDimensions(pX, pY, size.X, size.Y));
                    }
                }
            }

            foreach (var (image, _) in images)
            {
                image.Dispose();
            }

            RSI = rsi;
        }

        private static readonly JsonSchema RSISchema = GetSchema();

        private static JsonSchema GetSchema()
        {
            try
            {
                string schema;
                using (var schemaStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Robust.Client.Graphics.RSI.RSISchema.json"))
                using (var schemaReader = new StreamReader(schemaStream))
                {
                    schema = schemaReader.ReadToEnd();
                }

                return JsonSchema.FromJsonAsync(schema).Result;
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
