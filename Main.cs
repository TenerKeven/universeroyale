using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node
{
    public static ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
    public static Node PlayersNode;
    public static Node CharactersNode;
    public static Node MainNode;
    public static WorldSettings worldSettings;

    public override void _Ready()
    {
        PlayersNode = GetNode<Node>("Players");
        CharactersNode = GetNode<Node>("Characters");
        MainNode = GetTree().Root.GetChild(0);
        worldSettings = JSONReader<WorldSettings>.DeserializeFile("res://Shared/Settings/World.json");

        List<string> args = [.. OS.GetCmdlineArgs()];

        if (args.Contains("--client"))
        {
            AddChild(new ClientMain());
        }

        if (args.Contains("--server"))
        {
            AddChild(new ServerMain());
        }
    }
}
