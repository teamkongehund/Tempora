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
	/// A fraction that determines how of each left and right side of a visual block must be audio data from the preceding or next block.
	/// Useful to ensure the user always has a full view of a downbeat's transient peak.
	/// </summary>
	public float OverlapMargin = 0.1f;

	public int NumberOfBlocks = 10;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
}
