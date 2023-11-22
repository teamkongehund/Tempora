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

	private int _nominalMusicPositionStartForTopBlock = 0;
    public int NominalMusicPositionStartForTopBlock
	{
		get => _nominalMusicPositionStartForTopBlock;
		set
		{
			if (value != _nominalMusicPositionStartForTopBlock)
			{
				_nominalMusicPositionStartForTopBlock = value;
				UpdateBlocks();
			}
		}
	}

	private AudioFile _audioFile;
    public AudioFile AudioFile
	{
		get => _audioFile;
		set
		{
			if (_audioFile != value && _audioFile != null)
			{
				_audioFile = value;
				CreateBlocks();
			}
			else if (_audioFile != value)
			{
				_audioFile = value;
			}

        }
	}

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

			waveformWindow.NominalMusicPositionStartForWindow = musicPositionStart;
            musicPositionStart++;

			waveformWindow.SeekPlaybackTime += OnSeekPlaybackTime;
			waveformWindow.DoubleClicked += OnDoubleClick;
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
