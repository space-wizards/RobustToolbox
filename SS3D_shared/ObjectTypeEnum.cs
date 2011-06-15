using System.ComponentModel;
using System.Reflection;
using System;

public enum AtomType
{
    [AtomClassAttribute(typeof(SS3D_shared.AtomBaseClass))] 
    None = 0,

    [AtomClassAttribute(typeof(SS3D_shared.BaseTile))] 
    Tile,          //Walls, floors etc

    //[AtomClassAttribute(typeof(SS3D_shared.Item))] 
    //Item,          //Objects that can be picked up. Crowbar etc.

    //[AtomClassAttribute(typeof(SS3D_shared.Object))] 
    //Object,        //Objects that never can be picked up. Doors , computers etc.

    [AtomClassAttribute(typeof(SS3D_shared.AtomBaseClass))] 
    Mob            //Players. Returns atombaseclass until we have a playerclass.
}

public enum TileType
{
    [AtomClassAttribute(typeof(SS3D_shared.BaseTile))] 
    None = 0,

    [AtomClassAttribute(typeof(SS3D_shared.Floor))] 
    Floor,

    [AtomClassAttribute(typeof(SS3D_shared.Wall))] 
    Wall,

    [AtomClassAttribute(typeof(SS3D_shared.Space))] 
    Space
}

public enum ItemType
{
    //[AtomClassAttribute(typeof(SS3D_shared.Item))] 
    //None = 0,

    //[AtomClassAttribute(typeof(SS3D_shared.Crowbar))] 
    //Crowbar
}

public enum ObjectType
{
    //[AtomClassAttribute(typeof(SS3D_shared.Object))] 
    //None = 0
}

public enum MobType
{
    [AtomClassAttribute(typeof(SS3D_shared.AtomBaseClass))] 
    Mob       //Returns atombaseclass until we have a playerclass.
}

#region Enum Extensions
//These classes simply add a little wrapper for the attribute-reading methods. (So that you can call it directly from the enum).
public static class AtomTypeExtension
{
    /// <summary>
    /// Returns the Type of the class that was assigned to this enum.
    /// </summary>
    public static Type GetClass(this AtomType atomType)
    {
        return AtomEnumAtrribute.GetClass(atomType);
    }
}
public static class ItemTypeExtension
{
    /// <summary>
    /// Returns the Type of the class that was assigned to this enum.
    /// </summary>
    public static Type GetClass(this ItemType itemType)
    {
        return AtomEnumAtrribute.GetClass(itemType);
    }
}
public static class ObjectTypeExtension
{
    /// <summary>
    /// Returns the Type of the class that was assigned to this enum.
    /// </summary>
    public static Type GetClass(this ObjectType objectType)
    {
        return AtomEnumAtrribute.GetClass(objectType);
    }
}
public static class TileTypeExtension
{
    /// <summary>
    /// Returns the Type of the class that was assigned to this enum.
    /// </summary>
    public static Type GetClass(this TileType tileType)
    {
        return AtomEnumAtrribute.GetClass(tileType);
    }
}
public static class MobTypeExtension
{
    /// <summary>
    /// Returns the Type of the class that was assigned to this enum.
    /// </summary>
    public static Type GetClass(this MobType mobType)
    {
        return AtomEnumAtrribute.GetClass(mobType);
    }
} 
#endregion

public class AtomClassAttribute : Attribute
{
    //This class simply holds a Type. It's used to save the class type of the objects in the enums.
    public Type AtomClass;
    public AtomClassAttribute(Type type) {this.AtomClass = type;} //Simple constructor - sets the variable.
}

public static class AtomEnumAtrribute
{
    public static Type GetClass(Enum value) //We need an enum as input.
    {
        FieldInfo fieldInfo = value.GetType().GetField(value.ToString()); //Gets the field that contains the AtomClassAttribute.
        AtomClassAttribute[] attributes = (AtomClassAttribute[])fieldInfo.GetCustomAttributes(typeof(AtomClassAttribute), false); //Gets the custom AtomClassAttribute from the list of custom attributes.
        return (attributes[0].AtomClass); //Returns the AtomClass var of the custom AtomClassAttribute - which is a 'Type' of the corresponding class.
    }
}
