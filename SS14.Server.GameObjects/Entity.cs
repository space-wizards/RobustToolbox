using System.Linq;
using GameObject;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.Chat;
using ServerInterfaces.Configuration;
using ServerInterfaces.GOC;
using ServerInterfaces.MessageLogging;
namespace SGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    public class Entity : GameObject.Entity
    {
        #region Variables

        #region Delegates
        
        #endregion
        
        private readonly bool _messageProfiling;

        #endregion

        #region Constructor/Destructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public Entity(EntityManager entityManager)
            :base(entityManager)
        {
            _messageProfiling = IoCManager.Resolve<IConfigurationManager>().MessageLogging;
        }
        #endregion

        #region Component Manipulation

        /// <summary>
        /// Logs a component message to the messaging profiler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="type"></param>
        /// <param name="args"></param>
        private void LogComponentMessage(object sender, ComponentMessageType type, params object[] args)
        {
            if (!_messageProfiling)
                return;
            ComponentFamily senderfamily = ComponentFamily.Generic;
            int uid = 0;
            string sendertype = "";
            if (typeof (Component).IsAssignableFrom(sender.GetType()))
            {
                var realsender = (Component) sender;
                senderfamily = realsender.Family;

                uid = realsender.Owner.Uid;
                sendertype = realsender.GetType().ToString();
            }
            else
            {
                sendertype = sender.GetType().ToString();
            }
            //Log the message
            var logger = IoCManager.Resolve<IMessageLogger>();
            logger.LogComponentMessage(uid, senderfamily, sendertype, type);
        }

        #endregion

        /// <summary>
        /// Movement speed of the entity. This should be refactored.
        /// </summary>
        public float speed = 6.0f;

        #region Entity Members
        


        #endregion

        #region entity systems
        /// <summary>
        /// Match
        /// 
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool Match(IEntityQuery query)
        {
            // Empty queries always result in a match - equivalent to SELECT * FROM ENTITIES
            if (!(query.Exclusionset.Any() || query.OneSet.Any() || query.AllSet.Any()))
                return true;

            //If there is an EXCLUDE set, and the entity contains any component types in that set, or subtypes of them, the entity is excluded.
            bool matched = !(query.Exclusionset.Any() && query.Exclusionset.Any(t => ComponentTypes.Any(t.IsAssignableFrom)));
         
            //If there are no matching exclusions, and the entity matches the ALL set, the entity is included
            if(matched && (query.AllSet.Any() && query.AllSet.Any(t => !ComponentTypes.Any(t.IsAssignableFrom))))
                matched = false;
            //If the entity matches so far, and it matches the ONE set, it matches.
            if(matched && (query.OneSet.Any() && query.OneSet.Any(t => ComponentTypes.Any(t.IsAssignableFrom))))
                matched = false;
            return matched;
        }
        #endregion
        //VARIABLES TO REFACTOR AT A LATER DATE

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        public virtual void HandleClick(int clickerID)
        {
        }

        public void Emote(string emote)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Emote, emote, Name, Uid);
        }

        #region Networking


        #endregion
    }
}