using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Profiling;
using Robust.Shared.Random;
using Robust.Shared.Sandboxing;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Webp;

namespace Robust.Shared
{
    internal static class SharedIoC
    {
        public static void RegisterIoC(IDependencyCollection deps)
        {
            DisableTiffDecoding();

            deps.Register<ISerializationManager, SerializationManager>();
            deps.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            deps.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            deps.Register<IEntitySystemManager, EntitySystemManager>();
            deps.Register<ILogManager, LogManager>();
            deps.Register<IModLoader, ModLoader>();
            deps.Register<IModLoaderInternal, ModLoader>();
            deps.Register<INetManager, NetManager>();
            deps.Register<IRuntimeLog, RuntimeLog>();
            deps.Register<ITaskManager, TaskManager>();
            deps.Register<TaskManager, TaskManager>();
            deps.Register<ITimerManager, TimerManager>();
            deps.Register<ProfManager, ProfManager>();
            deps.Register<IRobustRandom, RobustRandom>();
            deps.Register<IRobustMappedStringSerializer, RobustMappedStringSerializer>();
            deps.Register<ISandboxHelper, SandboxHelper>();
            deps.Register<IManifoldManager, CollisionManager>();
            deps.Register<IVerticesSimplifier, RamerDouglasPeuckerSimplifier>();
            deps.Register<IParallelManager, ParallelManager>();
            deps.Register<IParallelManagerInternal, ParallelManager>();
            deps.Register<ToolshedManager>();
            deps.Register<HttpClientHolder>();
            deps.Register<RobustMemoryManager>();
            deps.Register<EntityConsoleHost>();
        }

        // Nothing in the process can decode TIFF, whose decoder is vulnerable
        private static void DisableTiffDecoding()
        {
            var formats = SixLabors.ImageSharp.Configuration.Default.ImageFormatsManager;
            formats.ClearImageFormatDetectors();
            formats.AddImageFormatDetector(new PngImageFormatDetector());
            formats.AddImageFormatDetector(new JpegImageFormatDetector());
            formats.AddImageFormatDetector(new GifImageFormatDetector());
            formats.AddImageFormatDetector(new BmpImageFormatDetector());
            formats.AddImageFormatDetector(new PbmImageFormatDetector());
            formats.AddImageFormatDetector(new TgaImageFormatDetector());
            formats.AddImageFormatDetector(new WebpImageFormatDetector());
            formats.AddImageFormatDetector(new QoiImageFormatDetector());
        }
    }
}
