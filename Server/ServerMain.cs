using Godot;
using System;

public partial class ServerMain : Node
{
    private PlayerHandler playerHandler;

    public override void _Ready()
    {
        Vector2 screenSize = DisplayServer.ScreenGetSize();
        Vector2 windowSize = DisplayServer.WindowGetSize();

        DisplayServer.WindowSetTitle("Servidor");
        DisplayServer.WindowSetPosition((Vector2I)new Vector2(screenSize.X - windowSize.X, 0));

        playerHandler = new PlayerHandler();

        Main.peer.CreateServer(4321, 2);

        Multiplayer.MultiplayerPeer = Main.peer;

        Main.peer.PeerConnected += OnPeerConnected;
        Main.peer.PeerDisconnected += OnPeerDisconnected;

        playerHandler.SetMultiplayerAuthority(1);

        Main.MainNode.AddChild(playerHandler);
    }

    private void OnPeerConnected(long peerId)
    {
        GD.Print($"Cliente conectado com ID: {peerId}");
    }

    private void OnPeerDisconnected(long peerId)
    {
        GD.Print($"Cliente desconectado com ID: {peerId}");
    }  
}
