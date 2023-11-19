using Godot;
using System;

public partial class Settings : Node
{
	public static Settings Instance;

	/// <summary>
	/// Musical grid divisor - can be thought of as 1/Divisor - i.e. a value of 4 means "display quarter notes"
	/// </summary>
	public int Divisor = 4;

	/// <summary>
	/// Number of waveform blocks to display
	/// </summary>
	public int NumberOfBlocks = 12;

	/// <summary>
	/// How many measures of overlapping time is added to the beginning and end of each waveform block
	/// </summary>
	public float MusicPositionMargin = 0.05f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
}
