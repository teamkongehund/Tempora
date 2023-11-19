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
    [Signal] public delegate void DoubleClickedEventHandler(float playbackTime);

    //public float FirstBlockStartTime = 0;

    public int NominalMusicPositionStartForTopBlock = 0;

    public AudioFile AudioFile;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		NumberOfBlocks = Settings.Instance.NumberOfBlocks;
        //	CreateBlocks();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
			{
                NominalMusicPositionStartForTopBlock += 1;
				UpdateBlocks();
                Signals.Instance.EmitSignal("Scrolled");
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
            {
                NominalMusicPositionStartForTopBlock -= 1;
				Signals.Instance.EmitSignal("Scrolled");
                UpdateBlocks();
            }
        }
    }

    public float BlockDuration = 2f;

    public void CreateBlocks()
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}

		//float startTime = FirstBlockStartTime;
		var packedWaveformWindow = ResourceLoader.Load<PackedScene>("res://WaveformWindow.tscn");

		// Instantiate block scenes and add as children
        for (int i = 0; i < NumberOfBlocks; i++) 
		{
			var waveformWindow = packedWaveformWindow.Instantiate();
			AddChild(waveformWindow);
		}

		int musicPositionStart = NominalMusicPositionStartForTopBlock;

		var children = GetChildren();

		foreach (WaveformWindow waveformWindow in children)
		{
			waveformWindow.AudioFile = AudioFile;

			waveformWindow.SizeFlagsVertical = SizeFlags.ExpandFill;

			//waveformWindow.StartTime = startTime;
			//waveformWindow.EndTime = startTime + BlockDuration;
			//startTime += BlockDuration;

			waveformWindow.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;

			waveformWindow.SeekPlaybackTime += OnSeekPlaybackTime;
			waveformWindow.DoubleClicked += OnDoubleClick;

			//GD.Print(waveformWindow.Size.Y);
		}
    }

	public void UpdateBlocks()
	{
		int musicPositionStart = NominalMusicPositionStartForTopBlock;

        var children = GetChildren();

        foreach (WaveformWindow waveformWindow in children)
		{
			waveformWindow.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;
		}
    }

	public void OnSeekPlaybackTime(float playbackTime)
	{
        EmitSignal(nameof(SeekPlaybackTime), playbackTime);
    }

    public void OnDoubleClick(float playbackTime)
    {
        EmitSignal(nameof(DoubleClicked), playbackTime);
    }
}
