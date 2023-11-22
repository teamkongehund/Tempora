using Godot;
using System;

public partial class Metronome : Node
{
	private double PreviousPlaybackTime = 0;
	private float? PreviousMusicPosition;

	Timing Timing;

	AudioStreamPlayer Click1;
    AudioStreamPlayer Click2;

	public bool On = true;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Timing = Timing.Instance;
		Click1 = GetNode<AudioStreamPlayer>("Click1");
        Click2 = GetNode<AudioStreamPlayer>("Click2");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Click(float musicPosition)
	{
		if (!On) return; 
		float beatPosition = Timing.GetBeatPosition(musicPosition);
		if (PreviousMusicPosition < beatPosition && musicPosition >= beatPosition)
		{
			if (beatPosition % 1 == 0)
			{
				Click1.Play();
			}
			else
			{
				Click2.Play();
			}
		}
		PreviousMusicPosition = musicPosition;
	}
}
