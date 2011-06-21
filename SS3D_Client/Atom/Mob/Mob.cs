using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mogre;
using Lidgren.Network;
using SS3D.Atom.Mob.HelperClasses;

namespace SS3D.Atom.Mob
{
    public class Mob : Atom
    {
        
        // TODO Make these some sort of well-organized global constant
        public float walkSpeed = 1.0f;
        public float runSpeed = 2.0f;

        public Dictionary<string, HelperClasses.Appendage> appendages;
        public Appendage selectedAppendage;

        //Current animation state -- or at least the one we want to add some time to. This will need to become more robust.
        public AnimationState animState;

        public Dictionary<string, AnimState> animStates;

        public Mob()
            : base()
        {
            meshName = "male_new.mesh";
        }

        public virtual void initAppendages()
        {
            appendages = new Dictionary<string, Appendage>();
            appendages.Add("LeftHand", new Appendage("Bip001 L Hand", "LeftHand", this));
            appendages.Add("RightHand", new Appendage("Bip001 R Hand", "RightHand", this));
            selectedAppendage = appendages["LeftHand"];
        }

        public override void SetUp(ushort _uid, AtomManager _atomManager)
        {
            base.SetUp(_uid, _atomManager);

            animState = Entity.GetAnimationState("idle1");
            animState.Loop = true;
            animState.Enabled = true;

            initAppendages();
        }

        /// <summary>
        /// Base atoms dont have animations, fuckface
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            InitAnimations();
        }

        public override void initKeys()
        {
            base.initKeys();

            keyHandlers.Add(MOIS.KeyCode.KC_F, new KeyEvent(HandleKC_F));
            keyHandlers.Add(MOIS.KeyCode.KC_Q, new KeyEvent(HandleKC_Q));
            keyHandlers.Add(MOIS.KeyCode.KC_LSHIFT, new KeyEvent(HandleKC_SHIFT));
            keyHandlers.Add(MOIS.KeyCode.KC_RSHIFT, new KeyEvent(HandleKC_SHIFT));
            
        }
        /// <summary>
        /// Initialize dictionary of animations. Also cocks.
        /// </summary>
        public virtual void InitAnimations()
        {
            animStates = new Dictionary<string, AnimState>();

            animStates.Add("death", new AnimState(Entity.GetAnimationState("death")));
            animStates.Add("tpose", new AnimState(Entity.GetAnimationState("tpose")));
            animStates.Add("walk1", new AnimState(Entity.GetAnimationState("walk1")));
            animStates.Add("idle1", new AnimState(Entity.GetAnimationState("idle1")));
            animStates.Add("rattack", new AnimState(Entity.GetAnimationState("rattack")));
            animStates.Add("lattack", new AnimState(Entity.GetAnimationState("lattack")));
        }

        public virtual void SetAnimationState(string state)
        {
            SetAnimationState(state, false);
        }

        public virtual void SetAnimationState(string state, bool send)
        {
            //return;
            //Disable old animation state.
            animState.Enabled = false;
            // TODO: error checking
            if (send)
                SendAnimationState(state);
            animState = Entity.GetAnimationState(state);
            animState.Loop = true;
            if (state == "tpose")
                animState.Loop = false;
            animState.Enabled = true;
            if (animState == null)
                animState = Entity.GetAnimationState("idle1");
        }

        protected virtual void SendAnimationState(string state)
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)MobMessage.AnimationState);
            message.Write(state);
            SendMessage(message);
        }

        protected virtual void HandleAnimationState(NetIncomingMessage message)
        {
            //If receiving animation state updates for our own mob, discard them.
            if(!attached)
                SetAnimationState(message.ReadString());
        }

        public override void Update()
        {
            base.Update();

            // Update Animation. Right now, anything animated will have to be updated in entirety every tick.
            TimeSpan t = atomManager.gameState.now - atomManager.gameState.lastUpdate; //LOL GOT IT BACKWRDS
            animState.AddTime((float)t.TotalMilliseconds / 1000f);
            var statestoupdate =
                from astate in animStates
                where astate.Value.enabled == true
                select astate.Value;

            foreach (AnimState a in statestoupdate)
                a.Update((float)t.TotalMilliseconds / 1000f);

            //Update every tick
            updateRequired = true;
        }

        public override void HandleKC_W(bool state)
        {
            base.HandleKC_W(state);
            if (state==true)
                SetAnimationState("walk1", true);
            else
                SetAnimationState("idle1", true);
        }
       
        public override void HandleKC_S(bool state)
        {
            base.HandleKC_S(state);
            if (state==true)
                SetAnimationState("walk1", true);
            else
                SetAnimationState("idle1", true);
        }

        public virtual void HandleKC_F(bool state)
        {
            if (state == true)
                SetAnimationState("walk1", true);
            else
                SetAnimationState("idle1", true);
        }

        public virtual void HandleKC_Q(bool state)
        {
            if (state == true)
                return;
            else
                SendDropItem();
        }

        public virtual void HandleKC_SHIFT(bool state)
        {
            if (state == true)
                speed = runSpeed;
            else
                speed = walkSpeed;
        }

        protected override void HandleExtendedMessage(NetIncomingMessage message)
        {
            MobMessage mobMessageType = (MobMessage)message.ReadByte();
            switch (mobMessageType)
            {
                case MobMessage.AnimationState:
                    HandleAnimationState(message);
                    break;
                case MobMessage.SelectAppendage:
                    HandleSelectAppendage(message);
                    break;
                case MobMessage.Death:
                    HandleDeath();
                    break;
                case MobMessage.AnimateOnce:
                    HandleAnimateOnce(message);
                    break;
                default: break;
            }
        }

        private void HandleDeath()
        {
            //Set death Animation
            SetAnimationState("death", true);

            //Clear key handlers
            keyHandlers.Clear();
            keyStates.Clear();
        }

        public virtual void HandleAnimateOnce(NetIncomingMessage message)
        {
            AnimState state = animStates[message.ReadString()];

            state.RunOnce();
        }

        /// <summary>
        /// Sets selected appendage to what is contained in the message
        /// </summary>
        /// <param name="message">Incoming netmessage</param>
        protected virtual void HandleSelectAppendage(NetIncomingMessage message)
        {
            SetSelectedAppendage(message.ReadString());
        }

        /// <summary>
        /// Sets selected appendage to the appendage named
        /// </summary>
        /// <param name="appendageName">Appendage name</param>
        protected virtual void SetSelectedAppendage(string appendageName)
        {
            if (appendages.Keys.Contains(appendageName))
                selectedAppendage = appendages[appendageName];
        }

        /// <summary>
        /// Sends a message to drop the item in the currently selected appendage
        /// </summary>
        protected virtual void SendDropItem()
        {
            NetOutgoingMessage message = CreateAtomMessage();
            message.Write((byte)AtomMessage.Extended);
            message.Write((byte)MobMessage.DropItem);
            SendMessage(message);
        }
    }
}
