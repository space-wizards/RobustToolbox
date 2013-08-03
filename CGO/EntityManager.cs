using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.Network;
using GameObject;
using Lidgren.Network;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager:GameObject.EntityManager
    {
        private Queue<IncomingEntityMessage> MessageBuffer = new Queue<IncomingEntityMessage>();


        public EntityManager(INetworkManager networkManager)
            :base(EngineType.Client, new EntityNetworkManager(networkManager))
        {
            Singleton = this;
        }

        private static EntityManager singleton;
        public static EntityManager Singleton
        {
            get
            {
                if (singleton == null) throw new Exception("Singleton not initialized");

                return singleton;
            }
            set
            { singleton = value; }
        }
        
        public Entity[] GetEntitiesInRange(Vector2D position, float Range)
        {
            var entities = from e in _entities.Values
                           where (position - e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position).Length < Range
                           select e;

            return entities.ToArray();
        }


        private void ProcessMsgBuffer()
        {
            if (!Initialized)
                return;
            if (!MessageBuffer.Any()) return;
            var misses = new List<IncomingEntityMessage>();

            while (MessageBuffer.Any())
            {
                IncomingEntityMessage entMsg = MessageBuffer.Dequeue();
                if (!_entities.ContainsKey(entMsg.Uid))
                {
                    entMsg.LastProcessingAttempt = DateTime.Now;
                    if ((entMsg.LastProcessingAttempt - entMsg.ReceivedTime).TotalSeconds > entMsg.Expires)
                        misses.Add(entMsg);
                }
                else
                    _entities[entMsg.Uid].HandleNetworkMessage(entMsg);
            }

            foreach (var miss in misses)
                MessageBuffer.Enqueue(miss);

            MessageBuffer.Clear(); //Should be empty at this point anyway.
        }

        private IncomingEntityMessage ProcessNetMessage(NetIncomingMessage msg)
        {
            return EntityNetworkManager.HandleEntityNetworkMessage(msg);
        }

        /// <summary>
        /// Handle an incoming network message by passing the message to the EntityNetworkManager 
        /// and handling the parsed result.
        /// </summary>
        /// <param name="msg">Incoming raw network message</param>
        public void HandleEntityNetworkMessage(NetIncomingMessage msg)
        {
            if (!Initialized)
            {
                var emsg = ProcessNetMessage(msg);
                if (emsg.MessageType != EntityMessage.Null)
                    MessageBuffer.Enqueue(emsg);
            }
            else
            {
                ProcessMsgBuffer();
                var emsg = ProcessNetMessage(msg);
                if (!_entities.ContainsKey(emsg.Uid))
                    MessageBuffer.Enqueue(emsg);
                else
                    _entities[emsg.Uid].HandleNetworkMessage(emsg);
            }
        }

        #region GameState Stuff
        public void ApplyEntityStates(List<EntityState> entityStates)
        {
            var entityKeys = new List<int>();
            foreach(var es in entityStates)
            {
                entityKeys.Add(es.StateData.Uid);
                //Known entities
                if(_entities.ContainsKey(es.StateData.Uid))
                {
                    _entities[es.StateData.Uid].HandleEntityState(es);
                }
                else //Unknown entities
                {
                    //SpawnEntityAt(es.StateData.TemplateName, es.StateData.Uid, es.StateData.Position);
                    Entity e = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
                    e.Name = es.StateData.Name;
                    e.HandleEntityState(es);
                }
            }

            //Delete entities that exist here but don't exist in the entity states
            var toDelete = _entities.Keys.Where(k => !entityKeys.Contains(k)).ToArray();
            foreach(var k in toDelete) 
                DeleteEntity(k);

            if(!Initialized)
                InitializeEntities();
        }

        public override void InitializeEntities()
        {
            base.InitializeEntities();
            Initialized = true;
        }
        #endregion
    }
}
