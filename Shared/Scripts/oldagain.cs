/*
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Character : CharacterBody3D
{
    [Export]
    protected Player player;
    protected RingBuffer<TransformState> transformStates;
    protected RingBuffer<InputState> inputStates;
    protected Queue<InputState> inputsQueue;
    protected Queue<TransformState> statesQueue;
    protected CharacterSettings characterSettings;
    protected bool initialized = false;
    protected double characterStarted;
    protected float ticksTime;
    protected int currentTick;
    protected InputState lastInputState;
    protected TransformState lastServerState;

    public Player Player
    {
        get { return player; }
    }

    public void Initialize(Player player)
    {
        currentTick = 0;
        inputsQueue = new Queue<InputState>();
        initialized = true;
        ticksTime = 1.0F / Main.worldSettings.TickRate;
        characterSettings = JSONReader<CharacterSettings>.DeserializeFile("res://Shared/Settings/Character.json");
        transformStates = new(characterSettings.MaxBufferSize);
        inputStates = new(characterSettings.MaxBufferSize);
        characterStarted =  Time.GetUnixTimeFromSystem();

       

        lastInputState = new InputState
        {
            InputDirection = new Vector2(0, 0),
            Tick = currentTick
        };

        inputStates.Set(currentTick, lastInputState);

        this.player = player;

        SetPhysicsProcess(true);
    }

    public override void _Ready()
    {
        GlobalPosition = new Vector3(0, 10, 0);

        if (!Multiplayer.IsServer())
        {
            RpcId(1, nameof(RequestedStartInfos));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!initialized) return;

        if (!Multiplayer.IsServer() && lastServerState.Processed)
        {
            Reconciliation(lastServerState);
            lastServerState.Processed = false;
        }

        SimulateEverything(ticksTime, currentTick, false);

        currentTick++;
    }

    //this just happens once, when the character is spawned
    public virtual void SpawnReconciliation(TransformState serverState, double latency)
    {
        if (serverState.Processed)
        {
            GlobalPosition = serverState.Position;
            Velocity = serverState.Velocity;

            transformStates.Set(currentTick, new TransformState
            {
                Tick = currentTick,
                Position = GlobalPosition,
                Velocity = Velocity,
                Processed = true
            });
        }


        int tickAmounts = Convert.ToInt32((float)latency / ticksTime);
        int count = 0;

        GD.Print(latency);

        while (count <= tickAmounts)
        {
            SimulateEverything(ticksTime, currentTick, true);
            currentTick++;
            count++;
        }
    }

    public virtual void SimulateEverything(float delta, int tick, bool reconciling)
    {
        SimulateGravity(delta, tick);

        Godot.Collections.Dictionary<long,int> clientsToReconciliating = new Godot.Collections.Dictionary<long, int>();

        if (!Multiplayer.IsServer())
        {
            if (!reconciling)
            {
                Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
                InputState newInputState = new InputState
                {
                    InputDirection = inputDirection,
                    Tick = tick
                };

                if (newInputState.InputDirection != lastInputState.InputDirection)
                {
                    RpcId(1, nameof(ClientInputReceived), newInputState.ToDictionary());
                }

                lastInputState = newInputState;
            }

            inputStates.Set(tick, lastInputState);
            SimulateMovement(delta, tick);
        }
        else
        {
            if (inputsQueue.Count == 0)
            {
                inputStates.Set(tick, lastInputState);
                SimulateMovement(delta, tick);
            }
            else
            {
                InputState currentState = inputsQueue.Dequeue();
                lastInputState = currentState;

                inputStates.Set(tick, currentState);
                SimulateMovement(delta, tick);

                clientsToReconciliating.Add(currentState.PeerId, currentState.Tick);
            }
        }

        MoveAndSlide();

        TransformState newState = new TransformState
        {
            Tick = tick,
            Position = GlobalPosition,
            Velocity = Velocity,
            Processed = true
        };

        transformStates.Set(tick, newState);

        if (Multiplayer.IsServer())
        {
            Dictionary stateDictionary = newState.ToDictionary();
            
            foreach (var value in clientsToReconciliating)
            {
                stateDictionary["Tick"] = value.Value;
                RpcId(value.Key, nameof(LocalClientReconciliation), stateDictionary);
            }
        }
    }

    public virtual void SimulateMovement(float delta, int tick)
    {
        InputState tickInputState = inputStates.Get(tick);
        Vector3 moveDirection = new(-tickInputState.InputDirection.X, 0, tickInputState.InputDirection.Y);
        Vector3 currentVelocity = Velocity;

        moveDirection = moveDirection.Normalized();
        moveDirection = new Vector3(moveDirection.X * (characterSettings.Speed * delta), 0, moveDirection.Z * (characterSettings.Speed * delta));

        Velocity = new Vector3(moveDirection.X, currentVelocity.Y, moveDirection.Z);
    }

    public virtual void SimulateGravity(float delta, int tick)
    {
        Vector3 gravityVector = new Vector3(0, Main.worldSettings.Gravity * delta, 0);
        Velocity -= gravityVector;
    }

    //the reconciliation code
    public virtual void Reconciliation(TransformState serverState)
    {
        TransformState clientState = transformStates.Get(serverState.Tick);
        float diferenceFromServer = serverState.Position.DistanceTo(clientState.Position);

        //GD.Print("client reconciliation " + serverState.Tick);

        if (diferenceFromServer >= 0.01f)
        {
            GD.Print("Difference studs from server is ", diferenceFromServer);
            GlobalPosition = serverState.Position;
            Velocity = serverState.Velocity;

            int count = serverState.Tick;

            while (count <= currentTick)
            {
                SimulateEverything(ticksTime, count, true);
                count++;
            }
        }
    }

    //Client received the start character position / when it started move
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ServerStartInfos(NodePath player, double serverTime, Dictionary transformDictionary)
    {
        if (Multiplayer.IsServer() || Multiplayer.GetRemoteSenderId() != 1) return;

        double receivedTime = TimeSynchronized.GetServerTime();
        double latency = receivedTime - serverTime;

        Initialize(GetNode(player) as Player);

        SpawnReconciliation(TransformState.ToStruct(transformDictionary), latency / 1000);
    }

    //Client received the message from server to reconciliate
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LocalClientReconciliation(Dictionary transformDictionary) {
        lastServerState = TransformState.ToStruct(transformDictionary);
    }

    //Server send for the client character current position / when it started move, just happens when the character spawn first time
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RequestedStartInfos()
    {
        long peerId = Multiplayer.GetRemoteSenderId();

        RpcId(peerId, nameof(ServerStartInfos), player.GetPath(), characterStarted, transformStates.Get(currentTick).ToDictionary());
    }

    //Client send for server the input
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientInputReceived(Dictionary inputDictionary)
    {
        InputState newInputState = InputState.ToStruct(inputDictionary);

        newInputState.PeerId = Multiplayer.GetRemoteSenderId();

        //GD.Print("clint sending ", newInputState.Tick);

        inputsQueue.Enqueue(newInputState);
    }
}
*/