using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using GorgonLibrary;
using System.Collections;
using Lidgren.Network;

namespace CGO
{
    /// <summary>
    /// Base entity class. Acts as a container for components, and a place to store location data.
    /// Should not contain any game logic whatsoever other than entity movement functions and 
    /// component management functions.
    /// </summary>
    public class Entity
    {
        #region Variables
        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private Dictionary<ComponentFamily, IGameObjectComponent> components;


        /// <summary>
        /// These are the only real pieces of data that the entity should have -- position and rotation.
        /// </summary>
        public Vector2D position;
        public float rotation;
        #endregion
        #region Constructor/Destructor
        /// <summary>
        /// Constructor
        /// </summary>
        public Entity()
        {
            components = new Dictionary<ComponentFamily, IGameObjectComponent>();
        }
        
        /// <summary>
        /// Shuts down the entity gracefully for removal.
        /// </summary>
        public void Shutdown()
        {
            foreach (GameObjectComponent component in components.Values)
            {
                component.OnRemove();
            }
            components.Clear();
        }
        #endregion

        #region Component Manipulation
        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        public void AddComponent(ComponentFamily family, IGameObjectComponent component)
        {
            if (components.Keys.Contains(family))
                RemoveComponent(family);
            components.Add(family, component);
            component.OnAdd(this); 
        }

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it 
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        public void RemoveComponent(ComponentFamily family)
        {
            if (components.Keys.Contains(family))
            {
                components[family].OnRemove();
                components.Remove(family); 
            }
        }

        /// <summary>
        /// Allows components to send messages
        /// </summary>
        /// <param name="sender">the component doing the sending</param>
        /// <param name="type">the type of message</param>
        /// <param name="args">message parameters</param>
        public void SendMessage(object sender, MessageType type, params object[] args)
        {
            foreach (IGameObjectComponent component in components.Values)
            {
                component.RecieveMessage(sender, type, args);
            }
        }
        #endregion

        /// <summary>
        /// Public update method for the entity. This will be useless after the atom code is refactored.
        /// </summary>
        /// <param name="frameTime"></param>
        public virtual void Update(float frameTime)
        {
        }


#region Movement
        /// <summary>
        /// Moves the entity to a new position in worldspace.
        /// </summary>
        /// <param name="toPosition"></param>
        public virtual void Translate(Vector2D toPosition)
        {
            Vector2D oldPosition = position;
            position += toPosition; // We move the sprite here rather than the position, as we can then use its updated AABB values.
        }

        /// <summary>
        /// Moves the entity Up
        /// </summary>
        public virtual void MoveUp()
        { }
        /// <summary>
        /// Moves the entity Down
        /// </summary>
        public virtual void MoveDown()
        { }
        /// <summary>
        /// Moves the entity Left
        /// </summary>
        public virtual void MoveLeft()
        { }
        /// <summary>
        /// Moves the entity Right
        /// </summary>
        public virtual void MoveRight()
        { }
        /// <summary>
        /// Moves the entity Up and Left
        /// </summary>
        public virtual void MoveUpLeft()
        { }
        /// <summary>
        /// Moves the entity Up and Right
        /// </summary>
        public virtual void MoveUpRight()
        { }
        /// <summary>
        /// Moves the entity Down and Left
        /// </summary>
        public virtual void MoveDownLeft()
        { }
        /// <summary>
        /// Moves the entity Down and Right
        /// </summary>
        public virtual void MoveDownRight()
        { }

#endregion
        //VARIABLES TO REFACTOR AT A LATER DATE
        /// <summary>
        /// Movement speed of the entity. This should be refactored.
        /// </summary>
        public float speed = 6.0f;

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        /// <summary>
        /// This should be refactored to some sort of component that sends entity movement input or something.
        /// </summary>
        public virtual void SendPositionUpdate()
        { }


    }
}
