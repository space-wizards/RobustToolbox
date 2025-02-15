using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Network.Messages;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using static Robust.Shared.Replays.ReplayMessage;

namespace Robust.Client.Replays.Playback;

// This partial class has code for performing tick updates (effectively the actual playback part of replays).
internal sealed partial class ReplayPlaybackManager
{
    public event IReplayPlaybackManager.HandleReplayMessageDelegate? HandleReplayMessage;

    private void TickUpdateOverride(FrameEventArgs args)
    {
        if (Replay == null)
        {
            _controller.TickUpdateOverride -= TickUpdateOverride;
            return;
        }

        if (ScrubbingTarget != null)
            SetIndex(ScrubbingTarget.Value, false);

        if (Replay.CurrentIndex + 1 >= Replay.States.Count)
            Playing = false;

        // TODO REPLAYS do we actually need to do this?
        // Either way, the UpdateFullRep() calls need to stay because it is needed for PVS-detached entities.
        _gameState.ResetPredictedEntities();

        if (Playing)
            Replay.CurrentIndex++;

        _timing.LastRealTick = _timing.LastProcessedTick = _timing.CurTick = Replay.CurTick;

        if (Playing)
        {
            var state = Replay.CurState;
            _gameState.UpdateFullRep(state, cloneDelta: true);
            var next = Replay.NextState;
            BeforeApplyState?.Invoke((state, next));
            _gameState.ApplyGameState(state, next);
            _gameState.MergeImplicitData();
            DebugTools.Assert(Replay.LastApplied >= state.FromSequence);
            DebugTools.Assert(Replay.LastApplied + 1 <= state.ToSequence);
            Replay.LastApplied = state.ToSequence;
            ProcessMessages(Replay.CurMessages, false);
        }

        _timing.CurTick += 1;
        _cEntManager.TickUpdate(args.DeltaSeconds, noPredictions: true);

        if (!Playing || AutoPauseCountdown == null)
            return;

        AutoPauseCountdown -= 1;
        if (AutoPauseCountdown <= 0)
        {
            Playing = false;
            AutoPauseCountdown = null;
        }
    }

    private void ProcessMessages(ReplayMessage replayMessageList, bool skipEffects)
    {
        if (Replay == null)
            throw new Exception("Not currently playing a replay");

        foreach (var message in replayMessageList.Messages)
        {
            if (message is CvarChangeMsg cvars)
            {
                _netMan.DispatchLocalNetMessage(new MsgConVars { Tick = _timing.CurTick, NetworkedVars = cvars.ReplicatedCvars });
                continue;
            }

            if (Replay.ClientSideRecording && message is LeavePvs leavePvs)
            {
                // TODO Replays detach immediate
                // Maybe track our own detach queue and use _gameState.DetachImmediate()?
                // That way we don't have to clone this. Downside would be that all entities will be immediately
                // detached. I.e., the detach budget cvar will simply be ignored.
                var clone = new List<NetEntity>(leavePvs.Entities);

                _gameState.QueuePvsDetach(clone, leavePvs.Tick);
                continue;
            }

            DebugTools.Assert(message is not LeavePvs);
            DebugTools.Assert(message is not ReplayPrototypeUploadMsg);
            DebugTools.Assert(message is not SharedNetworkResourceManager.ReplayResourceUploadMsg);

            if (HandleReplayMessage != null && HandleReplayMessage.Invoke(message, skipEffects))
                continue;

            if (message is EntityEventArgs args)
                _entMan.DispatchReceivedNetworkMsg(args);
            else if (_warned.Add(message.GetType()))
                _sawmill.Error($"Unhandled replay message: {message.GetType()}.");
        }
    }
}
