using Godot;
using Godot.Collections;
using System;

public struct InputState
{
    public int Tick { get; set; }
    public Vector2 InputDirection { get; set; }
    public long PeerId { get; set; }
    public bool Jumped { get; set; }

    public Dictionary ToDictionary()
    {
        return new Dictionary
        {
            {"Tick", Tick},
            {"InputDirection", InputDirection},
            {"PeerId", PeerId},
            {"Jumped", Jumped}
        };
    }

    public static InputState ToStruct(Dictionary dictionary)
    {
        return new InputState
        {
            Tick = (int)dictionary["Tick"],
            InputDirection = (Vector2)dictionary["InputDirection"],
            PeerId = (long)dictionary["PeerId"],
            Jumped = (bool)dictionary["Jumped"]
        };
    }
}
