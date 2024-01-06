using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.SpeechSynthesis;

internal sealed class TtsManager : ITtsManagerInternal, IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IDynamicTypeFactoryInternal _dynamicTypeFactory = default!;

    public bool Available => throw new NotImplementedException();
    public IReadOnlyList<ITtsVoice> Voices => throw new NotImplementedException();

    private ITtsManagerInternal _implementation = new TtsManagerDummy();

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        try
        {
            var sw = RStopwatch.StartNew();
            _implementation = InitializeSelectEngineCore();
            _sawmill.Verbose($"Initialized TTS system in {sw.Elapsed}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Exception while initializing TTS system!{e}");
        }
    }

    private ITtsManagerInternal InitializeSelectEngineCore()
    {
        ITtsManagerInternal engine;
#if WINDOWS
        engine = _dynamicTypeFactory.CreateInstanceUnchecked<TtsManagerWinRT>(oneOff: true);
#else
        engine = new TtsManagerDummy();
#endif
        engine.Initialize();
        return engine;
    }

    public void Speak(string text)
    {
        _implementation.Speak(text);
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("tts");
    }
}


