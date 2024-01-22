using System;
using Godot;

namespace Tempora.Classes.Utility;

public partial class TimeSignaturePoint(int[] timeSignature, int musicPosition) : Node, IComparable<TimeSignaturePoint>, ICloneable
{
    public int MusicPosition = musicPosition;
    public int[] TimeSignature = timeSignature;

    public int CompareTo(TimeSignaturePoint? other) => MusicPosition.CompareTo(other?.MusicPosition);

    public object Clone() => new TimeSignaturePoint(TimeSignature, MusicPosition);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() { }
}