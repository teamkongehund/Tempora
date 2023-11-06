using Godot;
using System;

public partial class Main : Control
{
	Button PlayButton;
	AudioPlayer AudioPlayer;
	WaveformWindow WaveformWindow;
	AudioVisualsContainer AudioVisualsContainer;
	Timing Timing;
	Button NudgeButton;

    string AudioPath = "res://UMO.mp3";

    AudioFile AudioFile;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		PlayButton = GetNode<Button>("PlayButton");
		AudioPlayer = GetNode<AudioPlayer>("AudioPlayer");
        AudioVisualsContainer = GetNode<AudioVisualsContainer>("AudioVisualsContainer");
		Timing = GetNode<Timing>("Timing");
		NudgeButton = GetNode<Button>("NudgeButton");
        //WaveformWindow = GetNode<WaveformWindow>("AudioVisualsContainer/WaveformWindow");

		AudioFile = new AudioFile(AudioPath);

		PlayButton.Pressed += Play;

		AudioPlayer.AudioFile = AudioFile;
        AudioPlayer.LoadMp3();

		AudioVisualsContainer.SeekPlaybackTime += OnPlaybackTimeClicked;
		AudioVisualsContainer.AddTimingPoint += OnAddTimingPoint;
		AudioVisualsContainer.AudioFile = AudioFile;
		AudioVisualsContainer.CreateBlocks();

		NudgeButton.Pressed += Nudge;
	}

	public void Nudge()
	{
        Timing.TimingPoints[1].MusicPosition += 0.25f;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdatePlayHeads();
	}

	public void Play()
	{
        foreach (WaveformWindow waveformWindow in AudioVisualsContainer.GetChildren())
        {
            GD.Print(waveformWindow.Size.Y);
        }
        AudioPlayer.Play();
	}

	public void UpdatePlayHeads()
	{
		double playbackTime = AudioPlayer.GetPlaybackTime();
		//float x = WaveformWindow.Waveforms[0].PlaybackTimeToPixelPosition((float)playbackTime);
		//WaveformWindow.Playhead.Position = new Vector2 (x, 0.0f);

		// Iterate through AudioVisualsContainer children and update Playheads
		foreach (WaveformWindow waveformWindow in AudioVisualsContainer.GetChildren())
		{
			foreach (var child in waveformWindow.GetChildren())
			{
				if (child is Waveform waveform)
				{
					float x = waveform.PlaybackTimeToPixelPosition((float)playbackTime);
					waveformWindow.Playhead.Position = new Vector2(x, 0.0f);
					waveformWindow.Playhead.Visible = (x >= 0 && x <= waveformWindow.Size.X);
				}
            }
        }
	}

	public void OnPlaybackTimeClicked(float playbackTime)
	{
		AudioPlayer.Seek(playbackTime);
	}

	public void OnAddTimingPoint(float playbackTime)
	{
		GD.Print($"Main: AddTimingPoint initiated");
		Timing.AddTimingPoint(playbackTime);
	}

}
