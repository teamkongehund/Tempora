using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Settings : Node
{
	public static Settings Instance;

	private int _divisor = 4;
	/// <summary>
	/// Musical grid divisor - can be thought of as 1/Divisor - i.e. a value of 4 means "display quarter notes"
	/// </summary>
	public int Divisor
	{
		get => _divisor;
		set
		{
			if (_divisor == value) return;
			_divisor = value;
            Signals.Instance.EmitSignal("SettingsChanged");
        }
	}

	public static readonly Dictionary<int, int> SliderToDivisorDict = new Dictionary<int, int>()
	{
		{ 1, 1 },
		{ 2, 2 },
		{ 3, 3 },
		{ 4, 4 },
		{ 5, 5 },
		{ 6, 6 },
		{ 7, 7 },
        { 8, 8 },
		{ 9, 9 },
		{ 10, 10 },
        { 11, 12 },
		{ 12, 16 }
    };

	public static int DivisorToSlider(int divisor) => SliderToDivisorDict.FirstOrDefault(x => x.Value == divisor).Key;

	private int _numberOfBlocks = 2 ;
	/// <summary>
	/// Number of waveform blocks to display
	/// </summary>
	public int NumberOfBlocks
	{
		get => _numberOfBlocks;
		set
		{
			if (_numberOfBlocks == value) return;
			_numberOfBlocks = value;
			Signals.Instance.EmitSignal("SettingsChanged");
		}
	}

	/// <summary>
	/// How many measures of overlapping time is added to the beginning and end of each waveform block
	/// </summary>
	public float MusicPositionMargin = 0.04f;

	/// <summary>
	/// Snap timing points to beat grid when moving them. 
	/// Should always be true because osu's timing points are always on-grid. 
	/// Probably best to exclude from any settings menu beacuse of this.
	/// </summary>
	public bool SnapToGridEnabled = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
}
