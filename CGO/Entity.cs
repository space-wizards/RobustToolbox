using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Reflection;
using GorgonLibrary;
using System.Collections;

namespace CGO
{
    public class Entity
    {
        private Dictionary<ComponentFamily, IGameObjectComponent> components;


        /// <summary>
        /// These are the only real pieces of data that the entity should have.
        /// </summary>
        public Vector2D position;
        public float rotation;

        public Entity()
        {
            components = new Dictionary<ComponentFamily, IGameObjectComponent>();
        }
        
        /// <summary>
        /// Public method to add a component to an entity.
        /// </summary>
        /// <param name="family"></param>
        /// <param name="component"></param>
        public void AddComponent(ComponentFamily family, IGameObjectComponent component)
        {
            if (components.Keys.Contains(family))
                RemoveComponent(family);
            components.Add(family, component);
            component.OnAdd(this);
        }

        public void RemoveComponent(ComponentFamily family)
        {
            components[family].OnRemove();
            components.Remove(family);
        }

        public virtual void Update(float frameTime)
        {
        }

        public void SendMessage(object sender, MessageType type, params object[] args)
        {
            foreach (IGameObjectComponent component in components.Values)
            {
                component.RecieveMessage(sender, type, args);
            }
        }

#region Movement
        public virtual void Translate(Vector2D toPosition)
        {
            Vector2D oldPosition = position;
            position += toPosition; // We move the sprite here rather than the position, as we can then use its updated AABB values.
        }

        public virtual void MoveUp()
        { }
        public virtual void MoveDown()
        { }
        public virtual void MoveLeft()
        { }
        public virtual void MoveRight()
        { }
        public virtual void MoveUpLeft()
        { }
        public virtual void MoveUpRight()
        { }
        public virtual void MoveDownLeft()
        { }
        public virtual void MoveDownRight()
        { }

#endregion
        //VARIABLES TO REFACTOR AT A LATER DATE
        public float speed = 6.0f;

        //FUNCTIONS TO REFACTOR AT A LATER DATE
        public virtual void SendPositionUpdate()
        { }


    }
}
