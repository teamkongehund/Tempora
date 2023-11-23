using Godot;
using System;

public partial class TimeSignaturePoint : Node, IComparable<TimeSignaturePoint>
{
	public int[] TimeSignature;

	public int MusicPosition;

	public TimeSignaturePoint(int[] timeSignature, int musicPosition)
	{
		TimeSignature = timeSignature;
		MusicPosition = musicPosition;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

    public int CompareTo(TimeSignaturePoint other) => MusicPosition.CompareTo(other.MusicPosition);
}
