using Godot;
using System;

public partial class Main : Control
{
	Button PlayButton;
    Button StopButton;
    Button MoveButton;
	
	TextEdit IndexField;
	TextEdit PositionField;

	BlockScrollBar BlockScrollBar;
	
	WaveformWindow WaveformWindow;
	
	AudioVisualsContainer AudioVisualsContainer;
	
	AudioPlayer AudioPlayer;
	
	Timing Timing;

    AudioFile AudioFile;

	Metronome Metronome;
    
	string AudioPath = "res://21csm.mp3";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		PlayButton = GetNode<Button>("PlayButton");
        StopButton = GetNode<Button>("StopButton");
        AudioPlayer = GetNode<AudioPlayer>("AudioPlayer");
        AudioVisualsContainer = GetNode<AudioVisualsContainer>("AudioVisualsContainer");
		//Timing = GetNode<Timing>("Timing");
		Timing = Timing.Instance;
		MoveButton = GetNode<Button>("MoveButton");
		IndexField = GetNode<TextEdit>("IndexField");
        PositionField = GetNode<TextEdit>("PositionField");
		Metronome = GetNode<Metronome>("Metronome");
		BlockScrollBar = GetNode<BlockScrollBar>("BlockScrollBar");

        AudioFile = new AudioFile(AudioPath);
		
		PlayButton.Pressed += Play;
		StopButton.Pressed += Stop;

		UpdateChildrensAudioFiles();

        AudioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
		AudioVisualsContainer.DoubleClicked += OnDoubleClick;
		AudioVisualsContainer.CreateBlocks();
		MoveButton.Pressed += OnMoveButtonPressed;
		Signals.Instance.Scrolled += OnScrolled;
		BlockScrollBar.ValueChanged += OnScrollBarValueChanged;
		GetTree().Root.FilesDropped += OnFilesDropped;

		UpdatePlayHeads();
		BlockScrollBar.UpdateMaxValue();
    }

    public override void _GuiInput(InputEvent @event)
    {
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
			{
				// Ensure a mouse release is always captured.
				GD.Print("Main: MouseLeftReleased");
				Signals.Instance.EmitSignal("MouseLeftReleased");
			}
		}
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		if (AudioPlayer.Playing)
		{
			UpdatePlayHeads();
			UpdateMetronome();
		}
    }

	public void OnFilesDropped(string[] filePaths)
	{
		if (filePaths.Length != 1) return;
		string path = filePaths[0];

		AudioFile audioFile;
		try
		{
			audioFile = new AudioFile(path);
			AudioFile = audioFile;
            UpdateChildrensAudioFiles();
        }
		catch { return; }
	}

    public void OnMoveButtonPressed()
	{
		int index = Int32.Parse(IndexField.Text);
        float position = float.Parse(PositionField.Text);

		TimingPoint timingPoint = Timing.TimingPoints[index];

		timingPoint.MusicPosition = position;
    }

	public void OnScrollBarValueChanged(double value)
	{
		AudioVisualsContainer.NominalMusicPositionStartForTopBlock = (int)value;
	}

	public void UpdateChildrensAudioFiles()
	{
        AudioPlayer.AudioFile = AudioFile;
        AudioVisualsContainer.AudioFile = AudioFile;
		Timing.AudioFile = AudioFile;
    }

	public void Play()
	{
        AudioPlayer.Play();
	}

	public void Stop()
	{
		AudioPlayer.Stop();
		UpdatePlayHeads();
    }

	public void OnScrolled()
	{
		UpdatePlayHeads();
		BlockScrollBar.Value = AudioVisualsContainer.NominalMusicPositionStartForTopBlock;
	}

	public void UpdatePlayHeads()
	{
		double playbackTime = AudioPlayer.GetPlaybackTime();
		float musicPosition = Timing.TimeToMusicPosition((float)playbackTime);
		foreach (WaveformWindow waveformWindow in AudioVisualsContainer.GetChildren())
		{
			float x = waveformWindow.MusicPositionToXPosition(musicPosition);
            waveformWindow.Playhead.Position = new Vector2(x, 0.0f);
            waveformWindow.Playhead.Visible = (x >= 0 && x <= waveformWindow.Size.X);
        }
	}

	public void UpdateMetronome()
	{
		double playbackTime = AudioPlayer.GetPlaybackTime();
        float musicPosition = Timing.TimeToMusicPosition((float)playbackTime);
		Metronome.Click(musicPosition);
    }

	public void OnSeekPlaybackTime(float playbackTime)
	{
		if (!AudioPlayer.Playing) { Play(); }
		if (playbackTime < 0) playbackTime = 0;
		AudioPlayer.Seek(playbackTime);
	}

	public void OnDoubleClick(float playbackTime)
	{
		TimingPoint timingPoint;
		Timing.AddTimingPoint(playbackTime, out timingPoint);
		Signals.Instance.HeldTimingPoint = timingPoint;
	}

}
