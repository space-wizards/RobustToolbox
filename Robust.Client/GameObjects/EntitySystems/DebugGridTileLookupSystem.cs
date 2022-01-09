#if DEBUG
using System;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects
{
    internal sealed class DebugGridTileLookupSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;

                if (_enabled)
                {
                    _label.Visible = true;
                    LastTile = default;
                }
                else
                {
                    _label.Text = null;
                    _label.Visible = false;
                }
            }
        }

        private bool _enabled;

        private (GridId Grid, Vector2i Indices) LastTile;

        // Label and shit that follows cursor
        private Label _label = new()
        {
            Visible = false,
        };

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<SendGridTileLookupMessage>(HandleSentEntities);
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.AddChild(_label);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            IoCManager.Resolve<IUserInterfaceManager>().StateRoot.RemoveChild(_label);
        }

        private void RequestEntities(GridId gridId, Vector2i indices)
        {
            if (gridId == GridId.Invalid) return;
            RaiseNetworkEvent(new RequestGridTileLookupMessage(gridId, indices));
        }

        private void HandleSentEntities(SendGridTileLookupMessage message)
        {
            if (!Enabled) return;
            var text = new StringBuilder();
            text.AppendLine($"GridId: {LastTile.Grid}, Tile: {LastTile.Indices}");

            for (var i = 0; i < message.Entities.Count; i++)
            {
                var entity = message.Entities[i];

                if (!EntityManager.EntityExists(entity)) continue;

                text.AppendLine((string) EntityManager.ToPrettyString(entity));
            }

            _label.Text = text.ToString();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            if (!Enabled) return;

            var mousePos = _inputManager.MouseScreenPosition;
            var worldPos = _eyeManager.ScreenToMap(mousePos);

            GridId gridId;
            Vector2i tile;

            if (_mapManager.TryFindGridAt(worldPos, out var grid))
            {
                gridId = grid.Index;
                tile = grid.WorldToTile(worldPos.Position);
            }
            else
            {
                gridId = GridId.Invalid;
                tile = new Vector2i((int) MathF.Floor(worldPos.Position.X), (int) MathF.Floor(worldPos.Position.Y));
            }

            LayoutContainer.SetPosition(_label, mousePos.Position);

            if ((gridId, tile).Equals(LastTile)) return;

            _label.Text = null;
            LastTile = (gridId, tile);
            RequestEntities(gridId, tile);
        }
    }

    internal sealed class RequestTileEntities : IConsoleCommand
    {
        public string Command => "tilelookup";
        public string Description => "Used for debugging GridTileLookupSystem";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<DebugGridTileLookupSystem>().Enabled ^= true;
        }
    }
}
#endif
