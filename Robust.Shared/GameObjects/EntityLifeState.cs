using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Shared.GameObjects
{
    public enum EntityLifeStage
    {
        /// <summary>
        /// The entity has just been created, and needs to be initialized.
        /// </summary>
        PreInit = 0,
        Initializing,
        Initialized,

        /// <summary>
        /// The entity is currently removing all of it's components and is about to be deleted.
        /// </summary>
        Terminating,

        /// <summary>
        /// The entity has been deleted.
        /// </summary>
        Deleted,
    }
}
