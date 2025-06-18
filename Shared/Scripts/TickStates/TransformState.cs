using Godot;
using Godot.Collections;
using System;

public struct TransformState
{
    public int Tick { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public double TimeStamp { get; set; }

    public Dictionary ToDictionary()
    {
        return new Dictionary
        {
            {"Tick", Tick},
            {"Position", Position},
            {"Velocity", Velocity},
            {"TimeStamp", TimeStamp}
        };
    }

    public static TransformState ToStruct(Dictionary dictionary)
    {
        return new TransformState
        {
            Tick = (int)dictionary["Tick"],
            Position = (Vector3)dictionary["Position"],
            Velocity = (Vector3)dictionary["Velocity"],
            TimeStamp = (double)dictionary["TimeStamp"]
        };
    }
}
