using System;
using Godot;

namespace OsuTimer.Classes.Utility;

public partial class TimeSignaturePoint : Node, IComparable<TimeSignaturePoint>, ICloneable
{
    public int MusicPosition;
    public int[] TimeSignature;

    public TimeSignaturePoint(int[] timeSignature, int musicPosition)
    {
        TimeSignature = timeSignature;
        MusicPosition = musicPosition;
    }

    public int CompareTo(TimeSignaturePoint? other)
    {
        return MusicPosition.CompareTo(other?.MusicPosition);
    }

    public object Clone()
    {
        return new TimeSignaturePoint(TimeSignature, MusicPosition);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }
}