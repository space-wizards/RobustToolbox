using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.GOC;
using GameObject;
using GorgonLibrary.InputDevices;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using CGO;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.UserInterface.Components
{
    public struct PartHp
    {
        public int currHP;
        public int maxHP;
    }

    class HealthScannerWindow : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly UserInterfaceManager _uiMgr;

        private readonly Sprite _background;

        private readonly TextSprite _overallHealth;

        private readonly Sprite _head;
        private readonly Sprite _chest;
        private readonly Sprite _arml;
        private readonly Sprite _armr;
        private readonly Sprite _groin;
        private readonly Sprite _legl;
        private readonly Sprite _legr;

        private readonly Entity assigned;

        private bool dragging = false;

        public HealthScannerWindow(Entity assignedEnt, Vector2D mousePos, UserInterfaceManager uiMgr, IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            assigned = assignedEnt;
            _uiMgr = uiMgr;

            _overallHealth = new TextSprite("hpscan" + assignedEnt.Uid.ToString(), "", _resourceManager.GetFont("CALIBRI"));
            _overallHealth.Color = Color.ForestGreen;

            _background = _resourceManager.GetSprite("healthscan_bg");

            _head = _resourceManager.GetSprite("healthscan_head");
            _chest = _resourceManager.GetSprite("healthscan_chest");
            _arml = _resourceManager.GetSprite("healthscan_arml");
            _armr = _resourceManager.GetSprite("healthscan_armr");
            _groin = _resourceManager.GetSprite("healthscan_groin");
            _legl = _resourceManager.GetSprite("healthscan_legl");
            _legr = _resourceManager.GetSprite("healthscan_legr");

            Position = new Point((int)mousePos.X, (int)mousePos.Y);

            Setup();
            Update(0);
        }

        private Color GetColor(int curr, int max)
        {
            var healthPct = curr / (float)max;

            if (healthPct > 0.75) return Color.LightGreen;
            else if (healthPct > 0.50) return Color.Yellow;
            else if (healthPct > 0.25) return Color.DarkOrange;
            else if (healthPct > 0) return Color.Red;
            else return Color.DimGray;
        }

        private void Setup()
        {
            var reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Head);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _head.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Torso);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _chest.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Left_Arm);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _arml.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Right_Arm);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _armr.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Groin);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _groin.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Left_Leg);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _legl.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            reply = assigned.SendMessage(this, ComponentFamily.Damageable, ComponentMessageType.GetCurrentLocationHealth, BodyPart.Right_Leg);
            if (reply.MessageType == ComponentMessageType.CurrentLocationHealth)
                _legr.Color = GetColor((int)reply.ParamsList[1], (int)reply.ParamsList[2]);

            if (assigned.HasComponent(ComponentFamily.Damageable))
            {
                var comp = (HealthComponent)assigned.GetComponent(ComponentFamily.Damageable);
                _overallHealth.Text = comp.GetHealth().ToString() + " / " + comp.GetMaxHealth().ToString();
            }

        }

        public override sealed void Update(float frameTime)
        {
            _background.Position = Position;

            _head.Position = Position;
            _chest.Position = Position;
            _arml.Position = Position;
            _armr.Position = Position;
            _groin.Position = Position;
            _legl.Position = Position;
            _legr.Position = Position;

            _overallHealth.Position = new Vector2D(Position.X + 86, Position.Y + 29);

            ClientArea = new Rectangle(Position, new Size((int)_background.AABB.Width, (int)_background.AABB.Height));
        }

        public override void Render()
        {
            _background.Draw();

            _head.Draw();
            _chest.Draw();
            _arml.Draw();
            _armr.Draw();
            _groin.Draw();
            _legl.Draw();
            _legr.Draw();

            _overallHealth.Draw();
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (dragging) Position = (Point)e.Position;
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                Vector2D insidePos = new Vector2D((int)e.Position.X - this.Position.X, (int)e.Position.Y - this.Position.Y);
                if ((insidePos - new Vector2D(189, 9)).Length <= 5)
                {
                    _uiMgr.RemoveComponent(this);
                    this.Dispose();
                }
                else if (insidePos.Y <= 18) dragging = true;
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                return true;
            }
            return false;
        }
    }
}
