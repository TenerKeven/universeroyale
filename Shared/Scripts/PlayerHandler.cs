using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerHandler : Node
{
    public static Dictionary<long, Player> players = [];
    public PackedScene playerScene = GD.Load<PackedScene>("res://Shared/Prefabs/Player.tscn");
    public MultiplayerSpawner playerSpawner;

    public override void _Ready()
    {
        playerSpawner = Main.MainNode.GetNode<MultiplayerSpawner>("PlayerSpawner");

        if (Multiplayer.IsServer())
        {
            Main.peer.PeerConnected += AddPlayer;
        }
        else
        {
            playerSpawner.Spawned += PlayerSpawned;
        }
    }

    public void AddPlayer(long peerId)
    {
        Player newPlayer = (Player)playerScene.Instantiate();

        newPlayer.SetMultiplayerAuthority((int)peerId, true);
        newPlayer.Initialize(peerId);
        players.Add(peerId, newPlayer);

        Main.PlayersNode.AddChild(newPlayer);
    }

    public void PlayerSpawned(Node PlayerNode)
    {
        
    }
}
