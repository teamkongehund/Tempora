using Godot;
using System;

public partial class AudioVisualsContainer : VBoxContainer
{
	[Export] public int NumberOfBlocks = 10;

	/// <summary>
	/// Forward childrens' signals to Main
	/// </summary>
	/// <param name="playbackTime"></param>
    [Signal] public delegate void SeekPlaybackTimeEventHandler(float playbackTime);
    [Signal] public delegate void AddTimingPointEventHandler(float playbackTime);

    public float FirstBlockStartTime = 0;

	public AudioFile AudioFile;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		NumberOfBlocks = Settings.Instance.NumberOfBlocks;
        //	CreateBlocks();
    }

	public float BlockDuration = 2f;

    public void CreateBlocks()
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}

		float startTime = FirstBlockStartTime;
		var packedWaveformWindow = ResourceLoader.Load<PackedScene>("res://WaveformWindow.tscn");

		// Instantiate block scenes and add as children
        for (int i = 0; i < NumberOfBlocks; i++) 
		{
			var waveformWindow = packedWaveformWindow.Instantiate();
			AddChild(waveformWindow);
		}

		var children = GetChildren();

		int musicPositionStart = 0;

		foreach (WaveformWindow waveformWindow in children)
		{
			waveformWindow.AudioFile = AudioFile;

			waveformWindow.SizeFlagsVertical = SizeFlags.ExpandFill;

			//waveformWindow.StartTime = startTime;
			//waveformWindow.EndTime = startTime + BlockDuration;
			//startTime += BlockDuration;

			waveformWindow.MusicPositionStart = musicPositionStart;
			musicPositionStart++;

			waveformWindow.SeekPlaybackTime += OnSeekPlaybackTime;
			waveformWindow.AddTimingPoint += OnAddTimingPoint;

			//GD.Print(waveformWindow.Size.Y);
		}
    }

	public void UpdateWaveforms()
	{
		foreach(WaveformWindow window in GetChildren())
		{
			window.RenderWaveforms();
		}
	}

	public void OnSeekPlaybackTime(float playbackTime)
	{
        EmitSignal(nameof(SeekPlaybackTime), playbackTime);
    }

    public void OnAddTimingPoint(float playbackTime)
    {
        EmitSignal(nameof(AddTimingPoint), playbackTime);
    }
}
