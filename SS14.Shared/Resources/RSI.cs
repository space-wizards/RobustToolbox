using Newtonsoft.Json.Linq;
using NJsonSchema;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace SS14.Shared.Resources
{
    /// <summary>
    ///     Type to load Robust Station Image (RSI) files.
    /// </summary>
    public sealed partial class RSI : IEnumerable<RSI.State>
    {
        /// <summary>
        ///     The minimum version of RSI we can load.
        /// </summary>
        public const uint MINIMUM_RSI_VERSION = 1;

        /// <summary>
        ///     The maximum version of RSI we can load.
        /// </summary>
        public const uint MAXIMUM_RSI_VERSION = 1;

        /// <summary>
        ///     The size of this RSI, width x height.
        /// </summary>
        public Vector2u Size { get; private set; }
        private Dictionary<StateId, State> States = new Dictionary<StateId, State>();

        public State this[StateId key]
        {
            get => States[key];
        }

        public void AddState(State state)
        {
            States[state.StateId] = state;
        }

        public void RemoveState(StateId stateId)
        {
            States.Remove(stateId);
        }

        public bool TryGetState(StateId stateId, out State state)
        {
            return States.TryGetValue(stateId, out state);
        }

        private RSI(Vector2u size)
        {
            Size = size;
        }

        /// <summary>
        ///     Loads an RSI from disk, returning the loaded RSI.
        /// </summary>
        /// <param name="filePath">The path on disk of the .rsi folder.</param>
        public static RSI FromDisk(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                throw new RSILoadException("Path must be a directory!");
            }

            var manifestPath = Path.Combine(filePath, "meta.json");
            if (!File.Exists(manifestPath))
            {
                throw new RSILoadException("Path is not an RSI: no meta.json");
            }

            string manifestContents;

            using (var manifestFile = File.OpenRead(manifestPath))
            using (var reader = new StreamReader(manifestFile))
            {
                manifestContents = reader.ReadToEnd();
            }
            
            var errors = RSISchema.Validate(manifestContents);
            if (errors.Count != 0)
            {
                Logger.Error($"Unable to load RSI from '{filePath}', {errors.Count} errors:");
                
                foreach (var error in errors)
                {
                    Logger.Error(error.ToString());
                }

                throw new RSILoadException($"{errors.Count} errors while loading RSI. See console.");
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
                State.DirectionType directions;

                switch (dirValue)
                {
                    case 1:
                        directions = State.DirectionType.Dir1;
                        break;
                    case 4:
                        directions = State.DirectionType.Dir4;
                        break;
                    case 8:
                        directions = State.DirectionType.Dir8;
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
                            delays[i] = new float[] { 1 };
                        }
                    }
                }
                else
                {
                    delays = new float[dirValue][];
                    // No delays specified, default to 1 frame per dir.
                    for (var i = 0; i < dirValue; i++)
                    {
                        delays[i] = new float[] { 1 };
                    }
                }

                // Cut out all the specific states.
                using (var stateImage = new Bitmap(Path.Combine(filePath, stateName) + ".png"))
                {
                    if (stateImage.Width % size.X != 0 || stateImage.Height % size.Y != 0)
                    {
                        throw new RSILoadException("State image size is not a multiple of the icon size.");
                    }

                    // Amount of icons per row of the sprite sheet.
                    var sheetWidth = stateImage.Width / size.X;

                    var iconFrames = new(Bitmap, float)[dirValue][];
                    var counter = 0;
                    for (var j = 0; j < iconFrames.Length; j++)
                    {
                        var delayList = delays[j];
                        var directionFrames = new(Bitmap, float)[delayList.Length];
                        for (var i = 0; i < delayList.Length; i++)
                        {
                            var sheetPosX = counter % sheetWidth;
                            var sheetPosY = counter / sheetWidth;
                            var rect = new Rectangle((int)(sheetPosX & size.X), 
                                                     (int)(sheetPosY * size.Y),
                                                     (int)size.X, (int)size.Y);

                            var bitmap = stateImage.Clone(rect, stateImage.PixelFormat);

                            directionFrames[i] = (bitmap, delayList[i]);
                            counter++;
                        }
                        iconFrames[j] = directionFrames;
                    }

                    var state = new State(size, stateName, directions, iconFrames);
                    rsi.AddState(state);
                }
            }

            return rsi;
        }

        private static readonly JsonSchema4 RSISchema = GetSchema();

        private static JsonSchema4 GetSchema()
        {
            string schema;
            using (var schemaStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SS14.Shared.Resources.RSISchema.json"))
            using (var schemaReader = new StreamReader(schemaStream))
            {
                schema = schemaReader.ReadToEnd();
            }

            return JsonSchema4.FromJsonAsync(schema).Result;
        }

        public IEnumerator<State> GetEnumerator()
        {
            return States.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Flags]
        public enum Selectors
        {
            None = 0,
        }

        /// <summary>
        ///     Represents a name+selector pair used to reference states in an RSI.
        /// </summary>
        public struct StateId
        {
            public readonly string Name;
            public readonly Selectors Selectors;

            public StateId(string name, Selectors selectors)
            {
                Name = name;
                Selectors = selectors;
            }

            public override string ToString()
            {
                return Name;
            }

            public static implicit operator StateId(string key)
            {
                return new StateId(key, Selectors.None);
            }

            public override bool Equals(object obj)
            {
                return obj is StateId id && Equals(id);
            }

            public bool Equals(StateId id)
            {
                return id.Name == Name && id.Selectors == Selectors;
            }

            public static bool operator ==(StateId a, StateId b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(StateId a, StateId b)
            {
                return !a.Equals(b);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode() ^ Selectors.GetHashCode();
            }
        }
    }


    [Serializable]
    public class RSILoadException : Exception
    {
        public RSILoadException() { }
        public RSILoadException(string message) : base(message) { }
        public RSILoadException(string message, Exception inner) : base(message, inner) { }
        protected RSILoadException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
    