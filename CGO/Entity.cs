using System;
using System.Collections.Generic;
using System.Linq;
using GameObject;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ClientInterfaces.MessageLogging;
using SS13.IoC;
using ClientInterfaces.Configuration;

namespace CGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    public class Entity : GameObject.Entity
    {
        #region Variables
        
        private bool _messageProfiling;
        
        #endregion

        #region Constructor/Destructor
        /// <summary>
        /// Constructor for realz. This one should be used eventually instead of the naked one.
        /// </summary>
        /// <param name="entityNetworkManager"></param>
        public Entity(EntityManager entityManager)
            :base(entityManager)
        {
            Initialize();

            var cfg = IoCManager.Resolve<IConfigurationManager>();
            _messageProfiling = cfg.GetMessageLogging();
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
            var senderfamily = ComponentFamily.Generic;
            var uid = 0;
            var sendertype = "";
            //if (sender.GetType().IsAssignableFrom(typeof(Component)))
            if (typeof(Component).IsAssignableFrom(sender.GetType()))
            {
                var realsender = (Component)sender;
                senderfamily = realsender.Family;

                uid = realsender.Owner.Uid;
                sendertype = realsender.GetType().ToString();
            }
            else
            {
                sendertype = sender.GetType().ToString();
            }
            //Log the message
            IMessageLogger logger = IoCManager.Resolve<IMessageLogger>();
            logger.LogComponentMessage(uid, senderfamily, sendertype, type);
        }

        #endregion
        
    }
}
