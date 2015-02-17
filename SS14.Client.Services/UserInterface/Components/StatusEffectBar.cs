using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System;
using System.Collections.Generic;
using System.Drawing;
using SFML.Window;
using SS14.Shared.Maths;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class StatusEffectBar : GuiComponent
    {
        private readonly IPlayerManager _playerManager;
        private readonly IResourceManager _resourceManager;
        private readonly List<StatusEffectButton> buttons = new List<StatusEffectButton>();

        private StatusEffectComp assigned;
        private Entity assignedEnt;

        private bool dragging;

        public StatusEffectBar(IResourceManager resourceManager, IPlayerManager playerManager)
        {
            _resourceManager = resourceManager;
            _playerManager = playerManager;

            if (playerManager.ControlledEntity != null &&
                playerManager.ControlledEntity.HasComponent(ComponentFamily.StatusEffects))
            {
                assignedEnt = playerManager.ControlledEntity;
                assigned = (StatusEffectComp) playerManager.ControlledEntity.GetComponent(ComponentFamily.StatusEffects);
                assigned.Changed += assigned_Changed;
            }

            UpdateButtons();
            Update(0);
            //Pull info from compo and sub to event.
        }

        private void UpdateButtons()
        {
            buttons.Clear();

            if (_playerManager.ControlledEntity != null && assigned != null)
            {
                foreach (StatusEffect effect in assigned.Effects)
                {
                    if (effect.isVisible == true)
                    {
                        var newButt = new StatusEffectButton(effect, _resourceManager);
                        buttons.Add(newButt);
                    }
                }
            }
        }

        private void assigned_Changed(StatusEffectComp sender)
        {
            UpdateButtons();
        }

        public override sealed void Update(float frameTime)
        {
            if (assignedEnt != null)
            {
                if (assignedEnt.Uid != _playerManager.ControlledEntity.Uid) //Seems like the controled ent changed.
                {
                    assigned.Changed -= assigned_Changed;
                    assigned = null;
                }
            }

            if (assigned == null && _playerManager.ControlledEntity != null &&
                _playerManager.ControlledEntity.HasComponent(ComponentFamily.StatusEffects))
            {
                assignedEnt = _playerManager.ControlledEntity;
                assigned =
                    (StatusEffectComp) _playerManager.ControlledEntity.GetComponent(ComponentFamily.StatusEffects);
                assigned.Changed += assigned_Changed;
                UpdateButtons();
            }

            if (buttons.Count > 0)
            {
                const int spacing = 3;
                const int max_per_row = 5;

                int curr_row_count = 0;
                int curr_row_num = 0;

                int x_off = spacing;
                int y_off = spacing;

                int max_x = 0;
                int max_y = 0;

                foreach (StatusEffectButton button in buttons)
                {
                    button.Position = new Point(Position.X + x_off, Position.Y + y_off);

                    if (Position.X + x_off + 32 + spacing > max_x) max_x = Position.X + x_off + 32 + spacing;
                    if (Position.Y + y_off + 32 + spacing > max_y) max_y = Position.Y + y_off + 32 + spacing;

                    if (curr_row_count >= (max_per_row - 1))
                    {
                        curr_row_num++;
                        curr_row_count = 0;
                        x_off = spacing + (spacing*curr_row_count) + (curr_row_count*32);
                        y_off = spacing + (spacing*curr_row_num) + (curr_row_num*32);
                    }
                    else
                    {
                        curr_row_count++;
                        x_off = spacing + (spacing*curr_row_count) + (curr_row_count*32);
                        y_off = spacing + (spacing*curr_row_num) + (curr_row_num*32);
                    }

                    button.Update(frameTime);
                }

                ClientArea = new Rectangle(Position, new Size(max_x - Position.X, max_y - Position.Y));
            }
        }

        public override void Render()
        {
            if (buttons.Count > 0)
                Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                     Color.DimGray);

            Gorgon.CurrentRenderTarget.Circle(Position.X, Position.Y, 3, Color.White);
            Gorgon.CurrentRenderTarget.Circle(Position.X, Position.Y, 2, Color.Gray);

            foreach (StatusEffectButton button in buttons)
                button.Render();

            foreach (StatusEffectButton button in buttons) //Needs to be separate so its drawn on top of all buttons.
                button.DrawTooltip();
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

		public override bool MouseDown(MouseButtonEventArgs e)
        {
            var mousePoint = new Vector2((int) e.Position.X, (int) e.Position.Y);

            if ((mousePoint - new Vector2(Position.X, Position.Y)).Length <= 3)
                dragging = true;

            return false;
        }

		public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                return true;
            }
            else
                return false;
        }

		public override void MouseMove(MouseMoveEventArgs e)
        {
            if (dragging)
            {
                Position = new Point((int) e.Position.X, (int) e.Position.Y);
            }
            else
            {
                foreach (StatusEffectButton button in buttons)
                {
                    button.MouseMove(e);
                }
            }
        }
    }
}