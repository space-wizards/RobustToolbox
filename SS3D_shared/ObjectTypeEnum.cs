using System.Reflection;
using System;

namespace SS13_Shared
{
    public enum TileType
    {
        None = 0,

        Floor,

        Wall,


        Space
    }

    public enum ItemType
    {

    }

    public enum ObjectType
    {

    }

    public enum MobType
    {
        [EntityClass(typeof(EntityBaseClass))] 
        Mob       //Returns entitybaseclass until we have a playerclass.
    }

    #region Enum Extensions
//These classes simply add a little wrapper for the attribute-reading methods. (So that you can call it directly from the enum).

    public static class ItemTypeExtension
    {
        /// <summary>
        /// Returns the Type of the class that was assigned to this enum.
        /// </summary>
        public static Type GetClass(this ItemType itemType)
        {
            return EntityEnumAtrribute.GetClass(itemType);
        }
    }
    public static class ObjectTypeExtension
    {
        /// <summary>
        /// Returns the Type of the class that was assigned to this enum.
        /// </summary>
        public static Type GetClass(this ObjectType objectType)
        {
            return EntityEnumAtrribute.GetClass(objectType);
        }
    }
    public static class TileTypeExtension
    {
        /// <summary>
        /// Returns the Type of the class that was assigned to this enum.
        /// </summary>
        public static Type GetClass(this TileType tileType)
        {
            return EntityEnumAtrribute.GetClass(tileType);
        }
    }
    public static class MobTypeExtension
    {
        /// <summary>
        /// Returns the Type of the class that was assigned to this enum.
        /// </summary>
        public static Type GetClass(this MobType mobType)
        {
            return EntityEnumAtrribute.GetClass(mobType);
        }
    } 
    #endregion

    public class EntityClassAttribute : Attribute
    {
        //This class simply holds a Type. It's used to save the class type of the objects in the enums.
        public Type EntityClass;
        public EntityClassAttribute(Type type) {this.EntityClass = type;} //Simple constructor - sets the variable.
    }

    public static class EntityEnumAtrribute
    {
        public static Type GetClass(Enum value) //We need an enum as input.
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString()); //Gets the field that contains the EntityClassAttribute.
            var attributes = (EntityClassAttribute[])fieldInfo.GetCustomAttributes(typeof(EntityClassAttribute), false); //Gets the custom EntityClassAttribute from the list of custom attributes.
            return (attributes[0].EntityClass); //Returns the EntityClass var of the custom EntityClassAttribute - which is a 'Type' of the corresponding class.
        }
    }
}