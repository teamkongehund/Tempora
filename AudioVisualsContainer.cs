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

	public float FirstBlockStartTime = 0;

	public AudioFile AudioFile;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		//CreateBlocks();
	}


    public void CreateBlocks()
	{
		float blockDuration = 2;
		float startTime = FirstBlockStartTime;
		var packedWaveformWindow = ResourceLoader.Load<PackedScene>("res://WaveformWindow.tscn");

		// Instantiate block scenes and add as children
        for (int i = 0; i < NumberOfBlocks; i++) 
		{
			var waveformWindow = packedWaveformWindow.Instantiate();
			AddChild(waveformWindow);
		}

		var children = GetChildren();

		foreach (WaveformWindow waveformWindow in children)
		{
			GD.Print(waveformWindow);

			waveformWindow.AudioFile = AudioFile;

			waveformWindow.SizeFlagsVertical = SizeFlags.ExpandFill;

			waveformWindow.startTime = startTime;
			waveformWindow.endTime = startTime + blockDuration;
			startTime += blockDuration;

			waveformWindow.SeekPlaybackTime += OnSeekPlaybackTime;

			//GD.Print(waveformWindow.Size.Y);
		}

        // Last, re-generate the waveforms once the windows have gotten their sizes.
        //foreach (WaveformWindow waveformWindow in children)
		//{
		//	waveformWindow.UpdateWaveform();
		//}

    }

	public void OnSeekPlaybackTime(float playbackTime)
	{
        EmitSignal(nameof(SeekPlaybackTime), playbackTime);
    }
}
