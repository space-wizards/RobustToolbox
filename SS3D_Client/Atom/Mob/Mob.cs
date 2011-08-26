using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SS3D.Atom.Mob.HelperClasses;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace SS3D.Atom.Mob
{
    public abstract class Mob : Atom
    {
        
        // TODO Make these some sort of well-organized global constant
        public float walkSpeed = 4.0f;
        public float runSpeed = 6.0f;

        public bool isDead = false;

        public Dictionary<string, HelperClasses.Appendage> appendages;
        public Appendage selectedAppendage;

        //Current animation state -- or at least the one we want to add some time to. This will need to become more robust.
        //public Mogre.AnimationState animState;
        public AnimState currentAnimState;
        
        public Dictionary<string, AnimState> animStates;

        public Mob()
            : base()
        {
            //meshName = "male_new.mesh";
            spritename = "Human";
            speed = walkSpeed;
        }

        public virtual void initAppendages()
        {
            appendages = new Dictionary<string, Appendage>();
            appendages.Add("LeftHand", new Appendage("Bip001 L Hand", "LeftHand", this));
            appendages.Add("RightHand", new Appendage("Bip001 R Hand", "RightHand", this));
            selectedAppendage = appendages["LeftHand"];
        }

        public virtual Item.Item GetItemOnAppendage(string appendage)
        {
            if (!appendages.ContainsKey(appendage)) return null;
            if (appendages[appendage] == null) return null;
            if (appendages[appendage].attachedItem == null) return null;
            else return appendages[appendage].attachedItem;
        }

        public override void SetUp(ushort _uid, AtomManager _atomManager)
        {
            base.SetUp(_uid, _atomManager);

            //currentAnimState = animStates["idle1"];
            //currentAnimState.Enable();
            //currentAnimState.LoopOn();
            /*animState = Entity.GetAnimationState("idle1");
            animState.Loop = true;
            animState.Enabled = true;*/

            sprite.UniformScale = 1f;
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

            keyHandlers.Add(KeyboardKeys.F, new KeyEvent(HandleKC_F));
            keyHandlers.Add(KeyboardKeys.Q, new KeyEvent(HandleKC_Q));
            keyHandlers.Add(KeyboardKeys.LShiftKey, new KeyEvent(HandleKC_SHIFT));
            keyHandlers.Add(KeyboardKeys.RShiftKey, new KeyEvent(HandleKC_SHIFT));
            
        }
        /// <summary>
        /// Initialize dictionary of animations. Also cocks.
        /// </summary>
        public virtual void InitAnimations()
        {
            animStates = new Dictionary<string, AnimState>();

            /*animStates.Add("death", new AnimState(Entity.GetAnimationState("death"), this));
            animStates.Add("tpose", new AnimState(Entity.GetAnimationState("tpose"), this));
            animStates.Add("walk1", new AnimState(Entity.GetAnimationState("walk1"), this));
            animStates.Add("idle1", new AnimState(Entity.GetAnimationState("idle1"), this));
            animStates.Add("rattack", new AnimState(Entity.GetAnimationState("rattack"), this));
            animStates.Add("lattack", new AnimState(Entity.GetAnimationState("lattack"), this));*/
        }

        public virtual void SetAnimationState(string state)
        {
            SetAnimationState(state, false);
        }

        public virtual void SetAnimationState(string state, bool send)
        {
            //return;
            //Disable old animation state.
            //currentAnimState.Disable();
            //currentAnimState.LoopOff();

            // TODO: error checking
            /*if (send)
                SendAnimationState(state);
            currentAnimState = animStates[state];
            currentAnimState.LoopOn();
            if (state == "tpose")
                currentAnimState.LoopOff();
            currentAnimState.Enable();
            if (currentAnimState == null)
                currentAnimState = animStates["idle1"];*/
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

        public override void Update(double time)
        {
            base.Update(time);

            // Update Animation. Right now, anything animated will have to be updated in entirety every tick.
            TimeSpan t = atomManager.gameState.now - atomManager.gameState.lastUpdate; //LOL GOT IT BACKWRDS
            //animState.AddTime((float)t.TotalMilliseconds / 1000f);
            var statestoupdate =
                from astate in animStates
                where astate.Value.enabled == true
                select astate.Value;

            foreach (AnimState a in statestoupdate)
                a.Update((float)t.TotalMilliseconds / 1000f);

            //Update every tick
            updateRequired = true;
        }

        /// <summary>
        /// Override to handle walk animations
        /// </summary>
        public override void UpdatePosition()
        {
            base.UpdatePosition();
            foreach (Appendage a in appendages.Values)
            {
                if (a.attachedItem != null)
                {
                    a.attachedItem.UpdatePosition();
                }
            }

            if (isDead)
                return;
            
            /*AnimState walk = animStates["walk1"];
            AnimState idle = animStates["idle1"];

            if (interpolationPackets.Count == 0)
            {
                walk.Disable();
                walk.LoopOff();
                idle.Enable();
                idle.LoopOn();
            }
            else
            {
                walk.Enable();
                walk.LoopOn();
                idle.Disable();
                idle.LoopOff();
            }*/

        }

        public override void HandleKC_W(bool state)
        {
            base.HandleKC_W(state);
            if (state==true)
                SetAnimationState("walk1", false);
            else
                SetAnimationState("idle1", false);
        }
       
        public override void HandleKC_S(bool state)
        {
            base.HandleKC_S(state);
            if (state==true)
                SetAnimationState("walk1", false);
            else
                SetAnimationState("idle1", false);
        }

        public virtual void HandleKC_F(bool state)
        {
            if (state == true)
                SetAnimationState("walk1", false);
            else
                SetAnimationState("idle1", false);
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
            isDead = true;
            //Set death Animation
            //SetAnimationState("death", true);
            DeathAnimation();

            //Clear key handlers
            keyHandlers.Clear();
            keyStates.Clear();
        }

        private void DeathAnimation()
        {
            /*DisableAllAnimationStates();
            AnimState deathstate = animStates["death"];
            deathstate.final = true;
            deathstate.tempdisabled = false;
            deathstate.Enable();
            deathstate.LoopOff();*/
        }

        public void DisableAllAnimationStates()
        {
            /*var statestodisable =
                from astate in animStates
                where astate.Value.enabled == true
                select astate.Value;

            foreach (AnimState a in statestodisable)
                a.Disable();*/
        }

        public virtual void HandleAnimateOnce(NetIncomingMessage message)
        {
            /*foreach (var s in animStates)
            {
                s.Value.tempdisabled = true;
            }
            AnimState state = animStates[message.ReadString()];

            state.RunOnce();*/
        }

        public virtual void AnimationComplete()
        {
            /*foreach (var s in animStates)
            {
                s.Value.tempdisabled = false;
            }*/
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
