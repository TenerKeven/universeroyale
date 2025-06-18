using Godot;
using System;

public partial class PlayerCharacter : Character
{
    protected bool newInputUpdate;
    public override void SetCurrentInput(int tick)
    {
        if (!Multiplayer.IsServer())
        {
            Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
            bool jumped = Input.IsActionJustPressed("jump");

            InputState newState = new InputState
            {
                InputDirection = inputDirection,
                Tick = tick,
                PeerId = player.PeerId,
                Jumped = jumped
            };

            if ((newState.InputDirection != lastInputState.InputDirection) || (newState.Jumped != lastInputState.Jumped))
            {
                newInputUpdate = true;
            }

            lastInputState = newState;
        }

        inputStates.Set(tick, lastInputState);
    }

    public override void ClientTick(float delta, int tick, bool reconciliating)
    {
        base.ClientTick(delta, tick, reconciliating);

        if (newInputUpdate && !reconciliating)
        {
            //GD.Print("update input server");
            newInputUpdate = false;

            RpcId(1, nameof(ClientInput), lastInputState.ToDictionary());
        }
    }

}
