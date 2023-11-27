using Godot;
using System;
using System.Collections.Generic;

public partial class AudioVisualsContainer : VBoxContainer
{
	//[Export] public int NumberOfBlocks = 10;

	/// <summary>
	/// Forward childrens' signals to Main
	/// </summary>
	/// <param name="playbackTime"></param>
    [Signal] public delegate void SeekPlaybackTimeEventHandler(float playbackTime);
    [Signal] public delegate void DoubleClickedEventHandler(float playbackTime);

	public List<WaveformWindow> WaveformWindows = new List<WaveformWindow>();

	//public float FirstBlockStartTime = 0;

	private int _nominalMusicPositionStartForTopBlock = 0;
    public int NominalMusicPositionStartForTopBlock
	{
		get => _nominalMusicPositionStartForTopBlock;
		set
		{
			if (value != _nominalMusicPositionStartForTopBlock)
			{
				_nominalMusicPositionStartForTopBlock = value;
				UpdateBlocksScroll();
			}
		}
	}

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		//NumberOfBlocks = Settings.Instance.NumberOfBlocks;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
			{
				NominalMusicPositionStartForTopBlock += Input.IsKeyPressed(Key.Shift) ? 5 : 1;
                Signals.Instance.EmitSignal("Scrolled");
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
            {
                NominalMusicPositionStartForTopBlock -= Input.IsKeyPressed(Key.Shift) ? 5 : 1;
                Signals.Instance.EmitSignal("Scrolled");
            }
        }
    }

	public void CreateBlocks()
	{
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        WaveformWindows.Clear();

        var packedWaveformWindow = ResourceLoader.Load<PackedScene>("res://Classes/Visual/WaveformWindow.tscn");

        // Instantiate block scenes and add as children
        for (int i = 0; i < Settings.Instance.MaxNumberOfBlocks; i++)
        {
            WaveformWindow waveformWindow = packedWaveformWindow.Instantiate() as WaveformWindow;
            AddChild(waveformWindow);
            WaveformWindows.Add(waveformWindow);
        }

        int musicPositionStart = NominalMusicPositionStartForTopBlock;

        foreach (WaveformWindow waveformWindow in WaveformWindows)
        {
            waveformWindow.SizeFlagsVertical = SizeFlags.ExpandFill;

            waveformWindow.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;

            waveformWindow.Playhead.Visible = false;

            waveformWindow.SeekPlaybackTime += OnSeekPlaybackTime;
            waveformWindow.AttemptToAddTimingPoint += OnDoubleClick;

            waveformWindow.IsInstantiating = false;
        }

		UpdateNumberOfBlocks();
    }

	public void UpdateNumberOfBlocks()
	{
        var children = GetChildren();

        for (int i = 0; i < children.Count; i++)
		{
			WaveformWindow waveformWindow = children[i] as WaveformWindow;
			waveformWindow.Visible = (i < Settings.Instance.NumberOfBlocks);
        }
    }


    public void UpdateBlocksScroll()
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
