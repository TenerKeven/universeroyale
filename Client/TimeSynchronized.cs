using Godot;
using System;

public partial class TimeSynchronized
{
    public double serverTime { get; set; }
    public double startedTime { get; set; }
    public double serverTimeDifference { get; set; }

    public void MakeSynchronization(double serverTime)
    {
        this.serverTime = serverTime;

        serverTimeDifference = Math.Max(serverTime - startedTime, 0);
    }

    public double GetServerTime()
    {
        return Time.GetTicksMsec() + serverTimeDifference; 
    }
}
