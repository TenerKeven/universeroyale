using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

public partial class Character : CharacterBody3D
{
    protected Player player;
    protected CharacterSettings characterSettings;
    protected TimeSynchronized synchronized;
    protected RingBuffer<TransformState> transformStates;
    protected RingBuffer<InputState> inputStates;
    protected Queue<long> playersToUpdateStartData;
    protected Queue<InputState> clientInputStates;
    protected TransformState spawnStateFromServer;
    protected TransformState lastServerSentState;
    protected TransformState lastServerProcessedState;
    protected InputState lastInputState;
    protected bool reconciliatePlayer;
    protected bool spawnReconciliated = false;
    protected bool initialized;
    protected float tickTime;
    protected int currentTick;
    protected float time;
    protected bool stop;

    public virtual void Initialize(Player player)
    {
        reconciliatePlayer = false;
        lastInputState = new InputState
        {
            InputDirection = new Vector2(0, 0),
            Tick = currentTick,
            PeerId = player.PeerId

        };
        playersToUpdateStartData = new Queue<long>();
        clientInputStates = new Queue<InputState>();
        synchronized = new TimeSynchronized();
        currentTick = 0;
        this.player = player;
        GlobalPosition = new Vector3(0, 8, 0);
        characterSettings = JSONReader<CharacterSettings>.DeserializeFile("res://Shared/Settings/Character.json");
        inputStates = new(characterSettings.MaxBufferSize);
        transformStates = new(characterSettings.MaxBufferSize);
        tickTime = 1f / Main.worldSettings.TickRate;
        initialized = true;
        synchronized.startedTime = Time.GetTicksMsec();

        transformStates.Set(currentTick, new TransformState
        {
            Tick = currentTick,
            Position = GlobalPosition,
            Velocity = Velocity,
            TimeStamp = synchronized.startedTime
        });

        inputStates.Set(currentTick, lastInputState);

        SetPhysicsProcess(true);
    }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, nameof(RequestServerStartInfos));
        }
    }

    public override void _Process(double delta)
    {
        if (stop) return;
        if (!initialized) return;
        time += (float)delta;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (stop) return;
        if (!initialized) return;

        while (time >= tickTime)
        {
            time -= tickTime;
            currentTick++;

            if (Multiplayer.IsServer())
            {
                ServerTick(tickTime, currentTick);
            }
            else
            {
                ClientTick(tickTime, currentTick, false);
            }
        }
    }

    public virtual void SimulateGravity(float delta, int tick)
    {
        Vector3 gravityVector = new Vector3(0, Main.worldSettings.Gravity * delta, 0);
        Velocity -= gravityVector;
    }

    public virtual void SimulateInputs(float delta, InputState inputState)
    {
        Vector2 inputDirection = inputState.Equals(default(InputState)) ? lastInputState.InputDirection : inputState.InputDirection;
        bool jumped = inputState.Equals(default(InputState)) ? lastInputState.Jumped : inputState.Jumped;
        Vector3 moveDirection = new(-inputDirection.X, 0, inputDirection.Y);
        Vector3 currentVelocity = Velocity;

        moveDirection = moveDirection.Normalized();
        moveDirection = new Vector3(moveDirection.X * (characterSettings.Speed * delta), jumped ? 5 : 0, moveDirection.Z * (characterSettings.Speed * delta));

        Velocity = new Vector3(moveDirection.X, currentVelocity.Y + moveDirection.Y, moveDirection.Z);
    }

    public virtual void SaveTick(int tick)
    {
        transformStates.Set(currentTick, new TransformState
        {
            Tick = tick,
            Position = GlobalPosition,
            Velocity = Velocity,
            TimeStamp = Time.GetTicksMsec()
        });
    }

    public virtual void ServerTick(float delta, int tick)
    {
        if (clientInputStates.Count > 0)
        {

            while (clientInputStates.Count > 0)
            {
                InputState currentInput = clientInputStates.Dequeue();
                lastInputState = currentInput;

                SimulateGravity(tickTime, currentTick);
                SetCurrentInput(currentTick);
                SimulateInputs(tickTime, lastInputState);
                MoveAndSlide();
                SaveTick(currentTick);

                if (clientInputStates.Count > 0)
                {
                    currentTick++;
                }
                
            }

            //GD.Print("Server chekignt ick is " + tickStart);
            //GD.Print("Server current tick is " + currentTick);

            RpcId(player.PeerId, nameof(ServerLastState), transformStates.Get(currentTick).ToDictionary(), lastInputState.Tick);

            return;
        }

        SimulateGravity(delta, tick);
        SetCurrentInput(tick);
        SimulateInputs(delta, lastInputState);
        MoveAndSlide();
        SaveTick(tick);

        while (playersToUpdateStartData.Count > 0)
        {
            long peerId = playersToUpdateStartData.Dequeue();

            RpcId(peerId, nameof(ServerStartInfosReceived), player.GetPath(), synchronized.startedTime, transformStates.Get(tick).ToDictionary());
        }
    }

    public virtual void ClientTick(float delta, int tick, bool reconciliating)
    {
        if (!spawnReconciliated && !spawnStateFromServer.Equals(default(TransformState)) && !spawnStateFromServer.Equals(lastServerProcessedState))
        {
            spawnReconciliated = true;
            lastServerProcessedState = spawnStateFromServer;
            SpawnReconciliation(lastServerProcessedState);
            return;
        }

        if (!reconciliating && !lastServerSentState.Equals(default(TransformState)) && !lastServerSentState.Equals(lastServerProcessedState))
        {
            lastServerProcessedState = lastServerSentState;

            Reconciliation(lastServerProcessedState);
        }

        SimulateGravity(delta, tick);
        if (!reconciliating)
        {
             SetCurrentInput(tick);
        }
        SimulateInputs(delta, reconciliating ? inputStates.Get(tick) : lastInputState);
        MoveAndSlide();
        SaveTick(tick);
    }

    public virtual void SpawnReconciliation(TransformState serverTransformState)
    {
        float latency = (float)((synchronized.GetServerTime() - serverTransformState.TimeStamp) / 1000);
        time = latency;

        GlobalPosition = serverTransformState.Position;
        Velocity = serverTransformState.Velocity;

        SaveTick(currentTick);

        while (time >= tickTime)
        {
            time -= tickTime;
            currentTick++;
            ClientTick(tickTime, currentTick, true);
        }
    }

    public virtual void Reconciliation(TransformState serverState)
    {
        int serverTick = serverState.Tick;
        TransformState clientState = transformStates.Get(serverTick);
        float distanceTo = serverState.Position.DistanceTo(clientState.Position);

        if (distanceTo >= 0.01f)
        {
            GD.Print("Server tick position is " + serverState.Position);
            GD.Print("Server tick velocity is " + serverState.Velocity);

            GD.Print("Client tick position is " + clientState.Position);
            GD.Print("Client tick velocity is " + clientState.Velocity);

            GD.Print(distanceTo);
            GlobalPosition = serverState.Position;
            Velocity = serverState.Velocity;

            SaveTick(serverState.Tick);

            if (serverTick + 1 >= currentTick) { return; }

            int count = serverTick + 1;

            while (count < currentTick)
            {
                //GD.Print("count is " + count);
                //GD.Print("current tick is " + currentTick);
                ClientTick(tickTime, count, true);
                count++;
            }
        }
    }

    public virtual void SetCurrentInput(int tick)
    {
        inputStates.Set(tick, lastInputState);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public virtual void RequestServerStartInfos()
    {
        playersToUpdateStartData.Enqueue(Multiplayer.GetRemoteSenderId());
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public virtual void ClientInput(Dictionary inputDictionary)
    {
        InputState clientInput = InputState.ToStruct(inputDictionary);

        clientInput.PeerId = Multiplayer.GetRemoteSenderId();

        clientInputStates.Enqueue(clientInput);
    }

    //ignore this
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public virtual void ServerStopPls(Dictionary serverState)
    {
        /*
        TransformState newState = TransformState.ToStruct(serverState);

        stop = true;

        GlobalPosition = newState.Position;
        Velocity = newState.Velocity;
        */
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public virtual void ServerStartInfosReceived(NodePath player, double serverCharacterStartedTime, Dictionary transformDictionary)
    {
        Initialize(GetNode(player) as Player);

        synchronized.MakeSynchronization(serverCharacterStartedTime);
        spawnStateFromServer = TransformState.ToStruct(transformDictionary);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public virtual void ServerLastState(Dictionary transformDictionary, int tick)
    {
        lastServerSentState = TransformState.ToStruct(transformDictionary);
        lastServerSentState.Tick = tick;
    }
}
