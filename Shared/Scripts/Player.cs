using Godot;
using System;

public partial class Player : Node
{
    [Export]
    protected long peerId;

    [Export]
    protected PlayerCharacter character;

    public PackedScene characterScene = GD.Load<PackedScene>("res://Shared/Prefabs/Character.tscn");

    public void Initialize(long peerId)
    {
        this.peerId = peerId;
    }

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            PlayerCharacter newCharacter = (PlayerCharacter)characterScene.Instantiate();

            newCharacter.SetMultiplayerAuthority((int)peerId, true);
            newCharacter.Initialize(this);
            character = newCharacter;

            Main.CharactersNode.AddChild(newCharacter);
        }
    }

    public long PeerId
    {
        get { return peerId; }
    }

}
