using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_shared.GO
{
    /// <summary>
    /// Component Family ie. what type of component it is.
    /// </summary>
    public enum ComponentFamily
    {
        Generic,
        Input, // Receives user input
        Mover, // Moves objects around
        Collider, // Handles collision with collidable stuff
        Collidable, // Handles being collided with
        Click, // Makes an object clickable
        Actor, // Allows an entity to interact with interactables
        Interactable, // Allows an entity to be interacted with.
        Intent,
        Inventory, // Holds entities
        Equipment, // ?
        Mob, // Has hands, can pick stuff up and manipulate objects
        Item, // Can be picked up, placed in inventory or held in hands
        LargeObject, // Can't be picked up, may or may not be able to move, can be interacted with
        Hands, // ? needed -- 
        Tool, // Can be used as a tool to apply to other entities
        Wearable, // Can be worn on a mob
        Health, // Has hitpoints, applies damage, organs?
        Renderable, // Can be rendered -- sprite or particle system
        Light,
        Damageable,
    }

    public enum ItemComponentNetMessage
    {
        PickedUp,
        Dropped,
    }

    public enum EquippableComponentNetMessage
    {
        UnEquipped,
        Equipped,
    }

    public enum ComponentMessageType
    {
        Empty,
        AddComponent,
        BoundKeyChange,
        BoundKeyRepeat,
        SlaveAttach,
        Click,
        SetSpriteByKey,
        IsCurrentHandEmpty,
        PickUpItem,
        Dropped,
        PickedUp,
        DisableCollision,
        EnableCollision,
        MoveDirection,
        HealthStatus,
        EntityChanged,
        ItemDetach,
        ItemWorn,
        ItemUnWorn,
        Clicked,
        SpriteChanged,
        CheckCollision,
        CollisionStatus,
        GetSprite,
        CurrentSprite,
        GetAABB,
        CurrentAABB,
        ClientInstantiated,
        GetMoveDir,
        SetDrawDepth,
        ActiveHandChanged,
        RecieveItemToActorInteraction,
        ActItemToItemInteraction,
        EnactItemToItemInteraction,
        ReceiveItemToActorInteraction,
        ReceiveItemToItemInteraction,
        ReceiveItemToLargeObjectInteraction,
        ReceiveEmptyHandToItemInteraction,
        ReceiveEmptyHandToActorInteraction,
        ReceiveEmptyHandToLargeObjectInteraction,
        EnactItemToActorInteraction,
        EnactItemToLargeObjectInteraction,
        GetCapability,
        ActiveHandItem,
        GetActiveHandItem,
        ItemGetCapability,
        CheckItemHasCapability,
        ItemGetAllCapabilities,
        ItemReturnCapability,
        ItemHasCapability,
        ItemGetCapabilityVerbPairs,
        ItemReturnCapabilityVerbPairs,
        ReturnActiveHandItem,
        ItemUnEquipped,
        ItemEquipped,
        EquipItem,
        UnEquipItem,
        UnEquipItemToFloor,
        UnEquipItemToHand,
        EquipItemInHand,
        DropItemInCurrentHand,
        Damage,
        GetArmorValues,
        ReturnArmorValues,
        Die,
    }

    public enum Hand
    {
        Left,
        Right
    }

    public enum DrawDepth
    {
        FloorTiles,
        FloorObjects,
        ItemsOnFloor,
        MobBase,
        MobUnderClothingLayer,
        MobOverClothingLayer,
        HeldItems,
        Tables,
        ItemsOnTables,
        Doors,
        Walls,
        WallMountedItems,
        WallTops,
        LightOverlay
    }
}
