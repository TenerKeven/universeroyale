using Godot;
using System;

public partial class ClientMain : Node
{
    private PlayerHandler playerHandler;
    public static long peerId;

    public override void _Ready()
    {
        Vector2 screenSize = DisplayServer.ScreenGetSize();
        Vector2 windowSize = DisplayServer.WindowGetSize();

        DisplayServer.WindowSetTitle("Cliente");
        DisplayServer.WindowSetPosition((Vector2I)new Vector2(0, (screenSize.Y - windowSize.Y) / 2));

        playerHandler = new PlayerHandler();

        Main.peer.CreateClient("127.0.0.1", 4321);
        Multiplayer.MultiplayerPeer = Main.peer;
        Multiplayer.ConnectedToServer += ConnectedToServer;

        /*
        Camera3D camera3D = new Camera3D
        {
            Name = "Camera",
            Current = true,
            Position = new Vector3(0, 5, 10)
        };
        camera3D.LookAt(Vector3.Zero, Vector3.Up);
        

        Main.MainNode.AddChild(camera3D);
        */
    }

    public void ConnectedToServer() {
        peerId = Multiplayer.GetUniqueId();

        playerHandler.SetMultiplayerAuthority((int) peerId, true);
    
        Main.MainNode.AddChild(playerHandler);
    }

}
