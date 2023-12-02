using Godot;
using System;

// Tempora

public partial class Main : Control
{
	Button ExportButton;
	Button ClearAllButton;
    //Button SaveButton;
	//Button LoadButton;
    //Button MoveButton;

    //TextEdit IndexField;
    //TextEdit PositionField;

    BlockScrollBar BlockScrollBar;

	HScrollBar GridScrollBar;
    HScrollBar PlaybackRateScrollBar;
    HScrollBar BlockAmountScrollBar;
	HScrollBar OffsetScrollBar;
    HScrollBar OverlapScrollBar;

    WaveformWindow WaveformWindow;
	
	AudioVisualsContainer AudioVisualsContainer;
	
	AudioPlayer AudioPlayer;


    //AudioFile AudioFile;

	ProjectFileManager ProjectFileManager;

	Metronome Metronome;
    
	string AudioPath = "res://Audio/21csm.mp3";

	// TODO 3: Add input field and/or number visualizer for dB on volume sliders

	// TODO 2: Add transient snapping:
	// A method finds the local loudest part of the song
	// Holding down a certain key combination and moving your mouse down through the transients will snap all of them when you release
	// So, whichever grid line you're closest to, all of them will snap to it - the grid line in question should light up.

	// TODO 2: Add offsetting option to timing points when you change time signature (keep measures or keep beats)

	// TODO 2: Add playhead in scrollbar

	// TODO 3: Update exisiting .osu files to synchronize to a specific beatmap folder.

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        ExportButton = GetNode<Button>("ExportButton");
        ClearAllButton = GetNode<Button>("ClearAllButton");
        //SaveButton = GetNode<Button>("SaveButton");
        //LoadButton = GetNode<Button>("LoadButton");
        AudioPlayer = GetNode<AudioPlayer>("AudioPlayer");
        AudioVisualsContainer = GetNode<AudioVisualsContainer>("AudioVisualsContainer");
		ProjectFileManager = ProjectFileManager.Instance;
        //MoveButton = GetNode<Button>("MoveButton");
        //IndexField = GetNode<TextEdit>("IndexField");
        //PositionField = GetNode<TextEdit>("PositionField");
        Metronome = GetNode<Metronome>("Metronome");
		BlockScrollBar = GetNode<BlockScrollBar>("BlockScrollBar");
        GridScrollBar = GetNode<HScrollBar>("SliderVBox/GridScrollBar");
        PlaybackRateScrollBar = GetNode<HScrollBar>("SliderVBox/PlaybackRateScrollBar");
		BlockAmountScrollBar = GetNode<HScrollBar>("SliderVBox/BlockAmountScrollBar");
        OffsetScrollBar = GetNode<HScrollBar>("SliderVBox/OffsetScrollBar");
        OverlapScrollBar = GetNode<HScrollBar>("SliderVBox/OverlapScrollBar");

        Project.Instance.AudioFile = new AudioFile(AudioPath);
		
		ExportButton.Pressed += OnExportButtonPressed;
		ClearAllButton.Pressed += OnClearAllButtonPressed;
        //SaveButton.Pressed += OnSaveButtonPressed;
        //LoadButton.Pressed += OnLoadButtonPressed;

        //UpdateChildrensAudioFiles();

        AudioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
		AudioVisualsContainer.DoubleClicked += OnDoubleClick;
		AudioVisualsContainer.CreateBlocks();
        //AudioVisualsContainer.CreateBlocks();
        //MoveButton.Pressed += OnMoveButtonPressed;
        Signals.Instance.Scrolled += OnScrolled;
		BlockScrollBar.ValueChanged += OnBlockScrollBarValueChanged;
		GetTree().Root.FilesDropped += OnFilesDropped;
		GridScrollBar.ValueChanged += OnGridScrollBarValueChanged;
		PlaybackRateScrollBar.ValueChanged += OnPlaybackRateScrollBarValueChanged;
        BlockAmountScrollBar.ValueChanged += OnBlockAmountScrollBarValueChanged;
		OffsetScrollBar.ValueChanged += OnOffsetScrollBarValueChanged;
        OverlapScrollBar.ValueChanged += OnOverlapScrollBarValueChanged;

        Signals.Instance.SettingsChanged += OnSettingsChanged;

        UpdatePlayHeads();
		BlockScrollBar.UpdateRange();
    }

    // TODO 2: Scroll to set BPM

    // TODO 2: Double / halve BPM for a point

    // TODO 2: Copy osu time stamp into app

    // TODO 2: Copy pasting groups of timing points

    // TODO 3: Detached reset points (points that work as metronome resets and don't force anything unto the previous.)

    // TODO 2: Spectral view (blackmann-harris rendering with 4096 bands or 2048 if it's too performance impacting)

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
                //GD.Print("Main: MouseLeftReleased");
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

	public void OnSettingsChanged()
	{
		AudioVisualsContainer.UpdateNumberOfBlocks();
	}

	public void OnFilesDropped(string[] filePaths)
	{
		if (filePaths.Length != 1) return;
		string path = filePaths[0];

		AudioFile audioFile;
		try
		{
			audioFile = new AudioFile(path);
			Project.Instance.AudioFile = audioFile;
			AudioPlayer.LoadMp3();
            //UpdateChildrensAudioFiles();
        }
		catch { return; }
	}

	public void OnExportButtonPressed()
	{
		ExportOsz();

		//ExportOsu();

		ExportButton.ReleaseFocus();
	}

    public void OnClearAllButtonPressed()
	{
		Timing.Instance.TimingPoints.Clear();
		Timing.Instance.TimeSignaturePoints.Clear();
        Signals.Instance.EmitSignal("TimingChanged");
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

	public void OnBlockAmountScrollBarValueChanged(double value)
	{
		int intValue = (int)value;
		Settings.Instance.NumberOfBlocks = intValue;
	}

	public void OnOffsetScrollBarValueChanged(double value)
	{
		Settings.Instance.MusicPositionOffset = (float)value;
	}

    public void OnOverlapScrollBarValueChanged(double value)
    {
        Settings.Instance.MusicPositionMargin = (float)value;
    }

    public void ExportOsu()
	{
		string path = "user://blob.osu";
        string dotOsu = OsuExporter.GetDotOsu(Timing.Instance);
        OsuExporter.SaveOsu(path, dotOsu);
    }

	public void ExportOsz()
	{
		Random random = new Random();
		int rand = random.Next();
        string path = $"user://{rand}.osz";
        string dotOsu = OsuExporter.GetDotOsu(Timing.Instance);
        OsuExporter.SaveOsz(path, dotOsu, Project.Instance.AudioFile);

		// Open with system:
		string globalPath = ProjectSettings.GlobalizePath(path);
		if (FileAccess.FileExists(globalPath))
        {
            OS.ShellOpen(globalPath);
        }
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
		float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
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
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
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
		Timing.Instance.AddTimingPoint(playbackTime, out timingPoint);
		if (timingPoint != null)
		{
			Context.Instance.HeldTimingPoint = timingPoint;
			Timing.SnapTimingPoint(timingPoint, (float)timingPoint.MusicPosition);
		}
	}

}
