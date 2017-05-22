namespace SS14.Shared.GameObjects
{
    /// <summary>
    /// Component Family ie. what type of component it is.
    /// </summary>
    public enum ComponentFamily
    {
        Null, //For message logger use
        Actor, // Allows an entity to interact with interactables
        Click, // Makes an object clickable
        Collider, // Handles collision with collidable stuff
        Collidable, // Handles being collided with
        ContextMenu,
        Damageable,
        Direction,
        EntityStats, //Holds stats an entity provides. Also Manages request concerning stats.
        Equipment, // ?
        Equippable,
        Generic,
        Hands, // ? needed --
        //Health, // Has hitpoints, applies damage, organs? NOT IN USE
        Hitbox,
        Icon,
        Input, // Receives user input
        Interactable, // Allows an entity to be interacted with.
        Intent,
        Inventory, //Holds entities
        Item, // Can be picked up, placed in inventory or held in hands
        LargeObject, // Can't be picked up, may or may not be able to move, can be interacted with
        Light,
        Mob, // Has hands, can pick stuff up and manipulate objects
        Mover, // Moves objects around
        Particles,
        Physics,
        PlayerActions, //Holds and manages player actions (abilities).
        Renderable, // Can be rendered -- sprite or particle system
        StatusEffects, //Holds and manages status effects.
        SVars,
        Think, // Holds more specific scripts that need to respond to event messages
        Tool, // Can be used as a tool to apply to other entities
        Transform,
        Velocity,
        WallMounted, //Provides methods to react to changing tiles. Intended for wall mounted objects.
        Wearable, // Can be worn on a mob
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
        DisassociateEntity,
        //All components that can hold entities must respond to this by dropping the entity to the floor and removing all references. They will also need to send this message when they aquire an entity so other components relinquish control of it.
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
        LeftClick,
        RightClick,
        AltLeftClick,
        AltRightClick,
        ShiftLeftClick,
        ShiftRightClick,
        CtrlLeftClick,
        CtrlRightClick,
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
        WearerIsDead,
        Live,
        WearerIsAlive,
        KilledEntity,
        ClickedInHand,
        SetLightState,
        Activate,
        Heal,
        SetBaseName,
        SetLightMode,
        WallMountTile,
        //WallMountSearch,
        PhysicsMove,
        GetHasInternals,
        GetSVars,
        SetSVar
    }

    public enum DrawDepth
    {
        FloorTiles = 0,
        FloorObjects = 1,
        ItemsOnFloor = 2,
        MobBase = 3,
        MobUnderClothingLayer = 4,
        MobUnderAccessoryLayer = 5,
        MobOverClothingLayer = 6,
        MobOverAccessoryLayer = 7,
        HeldItems = 8,
        Tables = 9,
        ItemsOnTables = 10,
        FloorPlaceable = 11,
        Doors = 12,
        Walls = 13,
        WallMountedItems = 14,
        WallTops = 15,
        LightOverlay = 16
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
