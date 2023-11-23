using Godot;
using System;

public partial class Main : Control
{
	Button ExportButton;
    //Button MoveButton;
	
	//TextEdit IndexField;
	//TextEdit PositionField;

	BlockScrollBar BlockScrollBar;

	HScrollBar GridScrollBar;
    HScrollBar PlaybackRateScrollBar;

    WaveformWindow WaveformWindow;
	
	AudioVisualsContainer AudioVisualsContainer;
	
	AudioPlayer AudioPlayer;
	
	Timing Timing;

    AudioFile AudioFile;

	Metronome Metronome;
    
	string AudioPath = "res://Audio/21csm.mp3";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        ExportButton = GetNode<Button>("ExportButton");
        AudioPlayer = GetNode<AudioPlayer>("AudioPlayer");
        AudioVisualsContainer = GetNode<AudioVisualsContainer>("AudioVisualsContainer");
		Timing = Timing.Instance;
		//MoveButton = GetNode<Button>("MoveButton");
		//IndexField = GetNode<TextEdit>("IndexField");
		//PositionField = GetNode<TextEdit>("PositionField");
		Metronome = GetNode<Metronome>("Metronome");
		BlockScrollBar = GetNode<BlockScrollBar>("BlockScrollBar");
        GridScrollBar = GetNode<HScrollBar>("GridScrollBar");
        PlaybackRateScrollBar = GetNode<HScrollBar>("PlaybackRateScrollBar");

        AudioFile = new AudioFile(AudioPath);
		
		ExportButton.Pressed += OnExportButtonPressed;

		UpdateChildrensAudioFiles();

        AudioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
		AudioVisualsContainer.DoubleClicked += OnDoubleClick;
		AudioVisualsContainer.CreateBlocks();
		//MoveButton.Pressed += OnMoveButtonPressed;
		Signals.Instance.Scrolled += OnScrolled;
		BlockScrollBar.ValueChanged += OnBlockScrollBarValueChanged;
		GetTree().Root.FilesDropped += OnFilesDropped;
		GridScrollBar.ValueChanged += OnGridScrollBarValueChanged;
		PlaybackRateScrollBar.ValueChanged += OnPlaybackRateScrollBarValueChanged;


        UpdatePlayHeads();
		BlockScrollBar.UpdateRange();
    }

	// TODO 1: Fix bug where you can add and snap a point right before an existing one

	// TODO 1: Save projects

	// TODO 2: Scroll to set BPM

	// TODO 1: Change time signature
	// Time signature is per measure, not per timing point
	// Each block should have a measure number and time signature on the left-hand side

	// TODO 2: Double / halve BPM for a point

	// TODO 2: Copy osu time stamp into app

	public override void _Input(InputEvent @event)
	{
        if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Keycode == Godot.Key.Space && keyEvent.Pressed)
			{
				PlayPause();
			}
		}
        if (@event is InputEventMouseButton mouseEvent)
        {
            //GD.Print("Main registered Input mouse event");
            //GrabFocus();
            //ReleaseFocus();
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
            {
                // Ensure a mouse release is always captured.
                GD.Print("Main: MouseLeftReleased");
                Signals.Instance.EmitSignal("MouseLeftReleased");
                Context.Instance.IsSelectedMusicPositionMoving = false;
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
            GrabFocus();
            ReleaseFocus();
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

	public void OnExportButtonPressed()
	{
		ExportOsz();

		//ExportOsu();

		ExportButton.ReleaseFocus();
	}
    
	public void OnBlockScrollBarValueChanged(double value)
	{
		AudioVisualsContainer.NominalMusicPositionStartForTopBlock = (int)value;
	}

	public void OnGridScrollBarValueChanged(double value)
	{
		int intValue = (int)value;
		Settings.Instance.Divisor = Settings.SliderToDivisorDict[intValue];
	}

	public void OnPlaybackRateScrollBarValueChanged(double value)
	{
		AudioPlayer.PitchScale = (float)value;
	}


    public void ExportOsu()
	{
		string path = "user://blob.osu";
        string dotOsu = OsuExporter.GetDotOsu(Timing);
        OsuExporter.SaveOsu(path, dotOsu);
    }

	public void ExportOsz()
	{
        string path = "user://exported.osz";
        string dotOsu = OsuExporter.GetDotOsu(Timing);
        OsuExporter.SaveOsz(path, dotOsu, AudioFile);

		// Open with system:
		string globalPath = ProjectSettings.GlobalizePath(path);
		if (FileAccess.FileExists(globalPath))
        {
            OS.ShellOpen(globalPath);
        }
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

    public void PlayPause()
    {
        AudioPlayer.PlayPause();
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
            waveformWindow.Playhead.Visible = (x >= 0 && x <= waveformWindow.Size.X) && AudioPlayer.Playing;
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
		if (timingPoint != null)
		{
			Context.Instance.HeldTimingPoint = timingPoint;
			Timing.SnapTimingPoint(timingPoint, (float)timingPoint.MusicPosition);
		}
	}

}
