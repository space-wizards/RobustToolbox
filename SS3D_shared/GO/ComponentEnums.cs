using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO
{
    /// <summary>
    /// Component Family ie. what type of component it is.
    /// </summary>
    public enum ComponentFamily
    {
        Null,   //For message logger use
        Generic,
        Input, // Receives user input
        Mover, // Moves objects around
        Collider, // Handles collision with collidable stuff
        Collidable, // Handles being collided with
        Click, // Makes an object clickable
        Actor, // Allows an entity to interact with interactables
        Interactable, // Allows an entity to be interacted with.
        Intent,
        Equipment, // ?
        Mob, // Has hands, can pick stuff up and manipulate objects
        Item, // Can be picked up, placed in inventory or held in hands
        Inventory, //Holds entities
        LargeObject, // Can't be picked up, may or may not be able to move, can be interacted with
        Hands, // ? needed -- 
        Tool, // Can be used as a tool to apply to other entities
        Wearable, // Can be worn on a mob
        //Health, // Has hitpoints, applies damage, organs? NOT IN USE
        Renderable, // Can be rendered -- sprite or particle system
        Light,
        Damageable,
        Equippable,
        WallMounted, //Provides methods to react to changing tiles. Intended for wall mounted objects.
        ContextMenu,
        Think, // Holds more specific scripts that need to respond to event messages
        StatusEffects, //Holds and manages status effects.
        PlayerActions, //Holds and manages player actions (abilities).
        EntityStats, //Holds stats an entity provides. Also Manages request concerning stats.
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
    
    public enum EquipmentComponentNetMessage
    {
        ItemEquipped,
        ItemUnEquipped
    }

    public enum ComponentMessageType
    {
        Null,
        DisassociateEntity, //All components that can hold entities must respond to this by dropping the entity to the floor and removing all references. They will also need to send this message when they aquire an entity so other components relinquish control of it.
        InventoryAdd,
        InventoryRemove,
        InventorySetSize,
        InventoryInformation,
        InventoryUpdateRequired,
        Empty,
        AddComponent,
        BoundKeyChange,
        BoundKeyRepeat,
        SlaveAttach,
        Click,
        SetSpriteByKey,
        SetVisible,
        IsCurrentHandEmpty,
        IsHandEmpty,
        IsHandEmptyReply,
        PickUpItem,
        PickUpItemToHand,
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
        CurrentLocationDamage,
        GetCurrentHealth,
        CurrentHealth,
        GetCurrentLocationHealth,
        CurrentLocationHealth,
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
        UnEquipItemToSpecifiedHand,
        EquipItemInHand,
        DropItemInCurrentHand,
        DropItemInHand,
        DropEntityInHand,
        Damage,
        GetArmorValues,
        ReturnArmorValues,
        Die,
        Bumped,
        CheckSpriteClick,
        SpriteWasClicked,
        Initialize,
        ReturnActorConnection,
        ReturnActorSession,
        GetActorConnection,
        GetActorSession,
        GetWearLoc,
        ReturnWearLoc,
        EquipItemToPart,
        GetItemInEquipmentSlot,
        ReturnItemInEquipmentSlot,
        ItemSlotEmpty,
        SetWornDrawDepth,
        HandsPickedUpItem,
        HandsDroppedItem,
        EntitySaidSomething,
        SendPositionUpdate, //makes mover component send position update to clients.
        ContextAdd,
        ContextRemove,
        ContextGetEntries,
        ContextMessage, //Sent when context menu option is clicked.
        GetDescriptionString, //Sent when description is requested and when answer is sent.
        AddStatusEffect,
        RemoveStatusEffect,
        AddAction,
        RemoveAction,
        CooldownAction,
        FailAction,
        RequestActionList,
        GetActionChecksum,
        DoAction,
        Incapacitated,
        NotIncapacitated,
        WearerIsDead,
        Live,
        WearerIsAlive,
        KilledEntity,
        ClickedInHand,
        SetLightState
    }

    public enum Hand
    {
        Left,
        Right,
        None
    }

    public enum DrawDepth
    {
        FloorTiles,
        FloorObjects,
        ItemsOnFloor,
        MobBase,
        MobUnderClothingLayer,
        MobUnderAccessoryLayer,
        MobOverClothingLayer,
        MobOverAccessoryLayer,
        HeldItems,
        Tables,
        ItemsOnTables,
        Doors,
        Walls,
        WallMountedItems,
        WallTops,
        LightOverlay
    }

    public enum StatusEffectFamily
    {
        None,
        Root,
        Stun,
        Snare,
        Damage,
        Healing
    }
}
