using System.Numerics;

namespace Robust.Shared3D;

/// <summary>
/// Minimal server-owned entity world used to establish the first networked 3D authority boundary.
/// </summary>
public sealed class AuthoritativeWorld3D
{
    public const float FixedDelta = 1f / 120f;

    private readonly Dictionary<int, PlayerEntity3D> _players = new();

    public long Tick { get; private set; }
    public IReadOnlyCollection<PlayerEntity3D> Players => _players.Values;

    public PlayerEntity3D AddPlayer(int playerId)
    {
        var player = new PlayerEntity3D(playerId, DemoWorld3D.GetPlayerSpawnPosition(playerId));
        _players.Add(playerId, player);
        return player;
    }

    public bool RemovePlayer(int playerId)
    {
        return _players.Remove(playerId);
    }

    public bool ApplyInput(int playerId, InputMessage3D input)
    {
        return _players.TryGetValue(playerId, out var player) && player.ApplyInput(input);
    }

    public void Step()
    {
        foreach (var player in _players.Values)
            player.Step(FixedDelta);

        Tick++;
    }

    public SnapshotMessage3D CreateSnapshot()
    {
        return new SnapshotMessage3D
        {
            ServerTick = Tick,
            Players = _players.Values
                .OrderBy(static player => player.PlayerId)
                .Select(static player => player.CreateSnapshot())
                .ToArray(),
        };
    }
}

public sealed class PlayerEntity3D
{
    private Vector2 _movement;
    private bool _jumpQueued;

    public int PlayerId { get; }
    public KinematicCharacter3D Character { get; }
    public float FacingYaw { get; private set; }
    public ulong AcknowledgedInput { get; private set; }

    public PlayerEntity3D(int playerId, Vector3 spawnPosition)
    {
        PlayerId = playerId;
        Character = new KinematicCharacter3D(spawnPosition, DemoWorld3D.CollisionBounds);
    }

    public bool ApplyInput(InputMessage3D input)
    {
        if (input.Sequence <= AcknowledgedInput)
            return false;

        var movement = new Vector2(input.MovementX, input.MovementY);
        if (!float.IsFinite(movement.X) || !float.IsFinite(movement.Y))
            movement = Vector2.Zero;
        if (movement.LengthSquared() > 1f)
            movement = Vector2.Normalize(movement);

        _movement = movement;
        _jumpQueued |= input.Jump;
        FacingYaw = float.IsFinite(input.FacingYaw) ? input.FacingYaw : 0f;
        AcknowledgedInput = input.Sequence;
        return true;
    }

    public void Step(float deltaTime)
    {
        Character.Step(new CharacterInput3D(_movement, _jumpQueued), deltaTime);
        _jumpQueued = false;
    }

    public PlayerSnapshot3D CreateSnapshot()
    {
        var position = Character.Position;
        var velocity = Character.Velocity;
        return new PlayerSnapshot3D
        {
            PlayerId = PlayerId,
            PositionX = position.X,
            PositionY = position.Y,
            PositionZ = position.Z,
            VelocityX = velocity.X,
            VelocityY = velocity.Y,
            VelocityZ = velocity.Z,
            FacingYaw = FacingYaw,
            Grounded = Character.IsGrounded,
            AcknowledgedInput = AcknowledgedInput,
        };
    }
}
