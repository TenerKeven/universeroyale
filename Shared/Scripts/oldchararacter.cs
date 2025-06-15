
/*
using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class OldCharacter : CharacterBody3D
{
    [Export]
    protected Player player;
    [Export]
    protected int tickRate = 60;
    protected int currentTick;
    protected float tickTime;
    protected float time;
    protected RingBuffer<CharacterState> characterStateBuffer = new(256);
    protected RingBuffer<InputState> inputStateBuffer = new(256);
    protected Queue<InputState> inputQueue = new Queue<InputState>();
    protected CharacterSettings characterSettings;
    protected InputState lastInputState;
    protected CharacterState latestServerState;
    protected CharacterState latestProcessedState;
    protected bool serverStateNotProcessed = false;
   
    public void Initialize(Player player)
    {
        this.player = player;
    }

    public override void _Ready()
    {
        tickTime = 1f / tickRate;
        characterSettings = JSONReader<CharacterSettings>.DeserializeFile("res://Shared/Settings/Character.json");
        Position = new Vector3(0, 5, 0);
    }

    public override void _Process(double delta)
    {
        time += (float)delta;

        while (time >= tickTime)
        {
            time -= tickTime;

            Tick();
            
            currentTick++;
        }
    }

    public virtual void Tick()
    {
        if (!Multiplayer.IsServer())
        {
            if (serverStateNotProcessed)
            {
                HandleServerReconciliation();
            }
            SetMovingDirection(tickTime);
        }
        else
        {
            SetServerMovingDirection(tickTime);
        }

        Simulate(tickTime);

        characterStateBuffer.Set(currentTick, new CharacterState
        {
            Tick = currentTick,
            Position = GlobalPosition,
            Velocity = Velocity
        });
    }

    public virtual void HandleServerReconciliation()
    {
        latestProcessedState = latestServerState;

        characterStateBuffer.TryGet(latestProcessedState.Tick, latestProcessedState.Tick, out CharacterState state);

        float positionDifference = latestProcessedState.Position.DistanceTo(state.Position);

        if (positionDifference > 0.01f)
        {
            GD.Print(positionDifference);
            GlobalPosition = latestProcessedState.Position;
            Velocity = latestProcessedState.Velocity;

            Simulate(tickTime);

            characterStateBuffer.Set(latestProcessedState.Tick, new CharacterState
            {
                Tick = latestServerState.Tick,
                Position = GlobalPosition,
                Velocity = Velocity
            });

            int newTicksToProcess = latestServerState.Tick + 1;

            while (newTicksToProcess < currentTick)
            {
                inputStateBuffer.TryGet(newTicksToProcess, newTicksToProcess, out InputState input);

                Velocity = CalculateMovingDirectionVelocity(tickTime, input.MoveDirection);

                Simulate(tickTime);

                CharacterState characterState = new CharacterState
                {
                    Tick = latestServerState.Tick,
                    Position = GlobalPosition,
                    Velocity = Velocity
                };

                characterStateBuffer.Set(newTicksToProcess, characterState);

                newTicksToProcess++;
            }
        }

        serverStateNotProcessed = false;
    }

    public virtual void SetMovingDirection(float delta)
    {
        Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

        Velocity = CalculateMovingDirectionVelocity(delta, inputDirection);

        InputState inputState = new InputState
        {
            Tick = currentTick,
            MoveDirection = inputDirection
        };

        if (inputState.MoveDirection != lastInputState.MoveDirection)
        {
            lastInputState = inputState;

            RpcId(1, nameof(ServerReceiveInput), inputState.ToDictionary());
        }
    }

    public virtual void SetServerMovingDirection(float delta)
    {
        if (!inputStateBuffer.TryGet(currentTick, currentTick, out InputState input))
        {
            inputStateBuffer.Set(currentTick, input);
        }

        int lastProcessedTick = -1;
        CharacterState lastCharacterState = new() { };

        while (inputQueue.Count > 0)
        {
            InputState inputState = inputQueue.Dequeue();

            Velocity = CalculateMovingDirectionVelocity(tickTime, inputState.MoveDirection);

            Simulate(tickTime);

            lastCharacterState = new CharacterState
            {
                Tick = inputState.Tick,
                Position = GlobalPosition,
                Velocity = Velocity
            };

            characterStateBuffer.Set(inputState.Tick, lastCharacterState);

            lastProcessedTick = inputState.Tick;
        }

        Velocity = CalculateMovingDirectionVelocity(tickTime, lastInputState.MoveDirection);

        if (lastProcessedTick != -1)
        {
            RpcId(player.PeerId, nameof(ClientReceiveServerState), lastCharacterState.ToDictionary());
        }
    }

    public virtual Vector3 CalculateMovingDirectionVelocity(float delta, Vector2 inputDirection)
    {
        Vector3 moveDirection = new(inputDirection.X, 0, inputDirection.Y);
        Vector3 currentVelocity = Velocity;

        moveDirection = moveDirection.Normalized();
        moveDirection = new Vector3(moveDirection.X * (characterSettings.Speed * delta), 0, moveDirection.Z * (characterSettings.Speed * delta));

        return new Vector3(moveDirection.X, currentVelocity.Y, moveDirection.Z);
    }

    public virtual void Simulate(float delta)
    {
        float frictionReduction = characterSettings.Friction * delta;
        Vector3 gravityVector = new(0, delta * Main.worldSettings.Gravity, 0);
        float newX = Velocity.X;
        float newZ = Velocity.Z;

        newX = newX > 0 ? MathF.Max(newX - frictionReduction, 0) : MathF.Min(newX + frictionReduction, 0);
        newZ = newZ > 0 ? MathF.Max(newZ - frictionReduction, 0) : MathF.Min(newZ + frictionReduction, 0);

        Velocity -= gravityVector;

        Vector3 frictionVector = new(newX, Velocity.Y, newZ);

        Velocity = frictionVector;

        MoveAndSlide();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    public async void ServerReceiveInput(Dictionary inputDictionary)
    {
        await ToSignal(GetTree().CreateTimer(0.02f), "timeout");
        lastInputState = InputState.ToStruct(inputDictionary);
        inputQueue.Enqueue(lastInputState);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public async void ClientReceiveServerState(Dictionary characterStateDictionary)
    {
        await ToSignal(GetTree().CreateTimer(0.02f), "timeout");
        latestServerState = CharacterState.ToStruct(characterStateDictionary);
        serverStateNotProcessed = true;
    }

    public Player Player
    {
        get { return player; }
    }
}
*/
