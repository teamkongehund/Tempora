using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Parent class for window containing waveform(s), playhead and timing grid
/// </summary>
public partial class WaveformWindow : Control
{
    #region Properties & Signals

    [Signal] public delegate void SeekPlaybackTimeEventHandler(float playbackTime);
	[Signal] public delegate void DoubleClickedEventHandler(float playbackTime);

    public Node2D WaveformFolder;
	public Node2D VisualTimingPointFolder;
    public Line2D Playhead;
	public Node2D GridFolder;
	public Line2D PreviewLine;
    public Line2D SelectedPositionLine;
    public Label MeasureLabel;
    public TimeSignatureLineEdit TimeSignatureLineEdit;


    private AudioFile _audioFile;
	public AudioFile AudioFile
	{
		get => _audioFile;
		set
		{
			if (_audioFile != value)
			{
				_audioFile = value;
				//UpdateWaveformAudioFiles();
				CreateWaveforms();
				RenderTimingPoints();
			}
        }
	}
	Waveform Waveform1;

	private int _musicPositionStart;
	public int NominalMusicPositionStartForWindow
	{
		get => _musicPositionStart;
		set
		{
			_musicPositionStart = value;
			UpdateTimingPointsIndices();
			CreateWaveforms();
            RenderTimingPoints();
            UpdateSelectedPositionLine();
            UpdateLabels();
            CreateGridLines();
        }
	}

	public float ActualMusicPositionStartForWindow
    {
        get => NominalMusicPositionStartForWindow - Settings.Instance.MusicPositionMargin;
        private set { }
    }

    /// <summary>
    /// List of horizontally stacked waveforms to display
    /// </summary>
    //public List<Waveform> Waveforms = new List<Waveform>();

    //public float StartTime = 0;
    //public float EndTime = 10;

    Timing Timing;

	public int FirstTimingPointIndex;
	public int LastTimingPointIndex;
    #endregion
    #region Godot & Signals

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		GridFolder = GetNode<Node2D>("GridFolder");
        WaveformFolder = GetNode<Node2D>("WaveformFolder");
        VisualTimingPointFolder = GetNode<Node2D>("VisualTimingPointFolder");
        PreviewLine = GetNode<Line2D>("PreviewLine");
        SelectedPositionLine = GetNode<Line2D>("SelectedPositionLine");
        MeasureLabel = GetNode<Label>("MeasureLabel");
        TimeSignatureLineEdit = GetNode<TimeSignatureLineEdit>("TimeSignatureLineEdit");

        Timing = Timing.Instance;
		
		Playhead = GetNode<Line2D>("Playhead");
        Playhead.Width = 4;
		Playhead.ZIndex = 100;
		UpdatePlayheadScaling();
		UpdatePreviewLineScaling();
        UpdateSelectedPositionScaling();
        UpdateSelectedPositionLine();

        Resized += OnResized;
		Signals.Instance.SettingsChanged += OnSettingsChanged;
        Signals.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
        Signals.Instance.Scrolled += UpdateSelectedPositionLine;

        TimeSignatureLineEdit.TimeSignatureSubmitted += OnTimingSignatureSubmitted;

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

        // If I used recommended += syntax here,
		// disposed WaveformWindows will still react to this signal, causing exceptions.
        // This seems to be a bug with the += syntax when the signal transmitter is an autoload
        // See https://github.com/godotengine/godot/issues/70414 (haven't read this through)
        Signals.Instance.Connect("TimingChanged", Callable.From(OnTimingChanged));
    }

	// Note to self: This can be used instead of .Connect to fix the signal issue.
	//public override void _ExitTree()
	//{
	//	Signals.Instance.TimingChanged -= OnTimingChanged;
	//}

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            //GD.Print("WaveformWindow is handling mouseEvent");
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                float x = mouseEvent.Position.X;
				float musicPosition = XPositionToMusicPosition(x);
				float time = Timing.MusicPositionToTime(musicPosition);
                GD.Print($"WaveformWindow was clicked at playback time {time} seconds");

                if (Input.IsKeyPressed(Key.Alt))
                {
                    EmitSignal(nameof(SeekPlaybackTime), time);
                }
                else
                {
                    Context.Instance.IsSelectedMusicPositionMoving = true;
                    Context.Instance.SelectedMusicPosition = XPositionToMusicPosition(x);
                }
            }
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && !Input.IsKeyPressed(Key.Alt))
            {
                float x = mouseEvent.Position.X;
                float musicPosition = XPositionToMusicPosition(x);
                float time = Timing.MusicPositionToTime(musicPosition);
                EmitSignal(nameof(DoubleClicked), time);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                float x = mouseEvent.Position.X;
                float musicPosition = XPositionToMusicPosition(x);
                float time = Timing.MusicPositionToTime(musicPosition);
                EmitSignal(nameof(SeekPlaybackTime), time);
            }
        }
		else if (@event is InputEventMouseMotion mouseMotion)
		{
            Vector2 mousePos = mouseMotion.Position;
            float mouseMusicPosition = XPositionToMusicPosition(mousePos.X);
            float mouseRelativeMusicPosition = XPositionToRelativeMusicPosition(mousePos.X);

            // HeldTimingPoint
            Timing.SnapTimingPoint(Context.Instance.HeldTimingPoint, mouseMusicPosition);

            // PreviewLine
            PreviewLine.Position = new Vector2(MusicPositionToXPosition(mouseMusicPosition), 0);

            // SelectedPosition
            if (!Context.Instance.IsSelectedMusicPositionMoving) return;
            Context.Instance.SelectedMusicPosition = XPositionToMusicPosition(mousePos.X);
        }
    }

	public void OnTimingChanged()
	{
		UpdateTimingPointsIndices();
		CreateWaveforms();
		RenderTimingPoints();
		CreateGridLines();
        UpdateLabels();
    }

    public void OnTimingSignatureSubmitted(int[] timeSignature)
    {
        Timing.Instance.UpdateTimeSignature(timeSignature, NominalMusicPositionStartForWindow);
    }
	public void OnResized() => UpdateVisuals();

    public void OnSettingsChanged() => UpdateVisuals();

    public void OnSelectedPositionChanged() => UpdateSelectedPositionLine();

    public void OnMouseEntered() => PreviewLine.Visible = true;

    public void OnMouseExited() => PreviewLine.Visible = false;

    #endregion
    #region Render

    public void CreateWaveforms()
	{
		//GD.Print($"{Time.GetTicksMsec()/1e3} - Now rendering waveform window {MusicPositionStart}!");

        foreach (var child in WaveformFolder.GetChildren())
        {
            if (child is Waveform waveform)
            {
                waveform.QueueFree();
            }
			else if (child is Sprite2D sprite)
			{
				sprite.QueueFree();
			}
        }

		float margin = Settings.Instance.MusicPositionMargin;

        float timeWhereWindowBegins = Timing.MusicPositionToTime(ActualMusicPositionStartForWindow);
        float timeWhereWindowEnds = Timing.MusicPositionToTime(ActualMusicPositionStartForWindow + 1 + 2 * margin);

        if (Timing.TimingPoints.Count == 0)
		{
			float startTime = timeWhereWindowBegins;
			float endTime = timeWhereWindowEnds;

            Waveform waveform = new Waveform(AudioFile, Size.X, Size.Y, new float[2] { startTime, endTime })
            {
                Position = new Vector2(0, Size.Y / 2),
            };

            WaveformFolder.AddChild(waveform);

			//GD.Print($"There were no timing points... Using MusicPositionStart {MusicPositionStartForWindow} to set start and end times.");

			return;
        }

		//GD.Print($"Window {MusicPositionStart}: timeWhereWindowBeginds = {timeWhereWindowBegins} , timeWhereWindowEnds = {timeWhereWindowEnds}");

		// Create each waveform segment
		for (int i = FirstTimingPointIndex; i <= LastTimingPointIndex; i++) 
		{
			//GD.Print($"Now rendering waveform for index {i}");
			TimingPoint timingPoint = Timing.TimingPoints[i];

			float startTime = (i == FirstTimingPointIndex)
				? timeWhereWindowBegins
                : (float)timingPoint.Time;

			float endTime = (i == LastTimingPointIndex)
				? timeWhereWindowEnds
                : (float)Timing.TimingPoints[i+1].Time;

            float musicPositionStart = Timing.TimeToMusicPosition(startTime);
			float musicPositionEnd = Timing.TimeToMusicPosition(endTime);

            float length = Size.X * (musicPositionEnd - musicPositionStart) / (1f + 2 * margin);
            float xPosition = Size.X * (musicPositionStart - ActualMusicPositionStartForWindow) / (1f + 2 * margin);

            Waveform waveform = new Waveform(AudioFile, length, Size.Y, new float[2] { startTime, endTime })
			{
				Position = new Vector2(xPosition, Size.Y / 2),
			};

            // Randomize color, so it's easy to see what's happening
            //Random random = new Random();
            //waveform.DefaultColor = new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1);

            WaveformFolder.AddChild(waveform);
        }
	}

	public void RenderTimingPoints()
	{
        foreach (var child in VisualTimingPointFolder.GetChildren())
        {
            if (child is VisualTimingPoint visualTimingPoint)
                visualTimingPoint.QueueFree();
        }
        foreach (TimingPoint timingPoint in Timing.TimingPoints)
		{
            var packedVisualTimingPoint = ResourceLoader.Load<PackedScene>("res://Classes/Visual/VisualTimingPoint.tscn");
            if (timingPoint.MusicPosition >= ActualMusicPositionStartForWindow 
				&& timingPoint.MusicPosition < ActualMusicPositionStartForWindow + 1 + 2 * Settings.Instance.MusicPositionMargin)
			{
				VisualTimingPoint visualTimingPoint = packedVisualTimingPoint.Instantiate() as VisualTimingPoint;
				float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);
				visualTimingPoint.TimingPoint = timingPoint;
                visualTimingPoint.Position = new Vector2(x, Size.Y / 2);
				visualTimingPoint.Scale = new Vector2(0.2f, 0.2f);
                VisualTimingPointFolder.AddChild(visualTimingPoint);
			}
		}
	}
	public void CreateGridLines()
	{
		foreach (var child in GridFolder.GetChildren())
		{
			child.QueueFree();
		}

		int divisor = Settings.Instance.Divisor;
        int[] timeSignature = Timing.GetTimeSignature(NominalMusicPositionStartForWindow);

		int divisionIndex = 0;
		float latestPosition = 0;
		while (latestPosition < 1)
		{
			if (divisionIndex >= 50) throw new Exception("Too many measure divisions!");

			GridLine gridLine = GetGridLine(timeSignature, divisor, divisionIndex);

			if (gridLine == null || gridLine.RelativeMusicPosition > 1) break;

			gridLine.ZIndex = 99;

			divisionIndex++;

			GridFolder.AddChild(gridLine);
		}
	}
	public GridLine GetGridLine(int[] timeSignature, int divisor, int index)
	{
		GridLine gridLine = new GridLine(timeSignature, divisor, index);

        float margin = Settings.Instance.MusicPositionMargin;
        float xPosition = Size.X * ((gridLine.RelativeMusicPosition + margin) / (2 * margin + 1f));
        gridLine.Position = new Vector2(xPosition, 0);
        gridLine.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };

        return gridLine;
	}

    #endregion
    #region Updaters
    public void UpdateVisuals()
    {
        UpdatePlayheadScaling();
        UpdatePreviewLineScaling();
        UpdateSelectedPositionScaling();
        CreateWaveforms();
        RenderTimingPoints();
        CreateGridLines();
        UpdateLabels();
    }

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.GetTimeSignature(NominalMusicPositionStartForWindow);
        MeasureLabel.Text = NominalMusicPositionStartForWindow.ToString();
        TimeSignatureLineEdit.Text = $"{timeSignature[0]}/{timeSignature[1]}";
    }

    public void UpdatePlayheadScaling()
	{
        Playhead.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
    }

	public void UpdatePreviewLineScaling()
	{
        PreviewLine.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
    }

    public void UpdateSelectedPositionLine()
    {
        float x = MusicPositionToXPosition(Context.Instance.SelectedMusicPosition);
        SelectedPositionLine.Position = new Vector2(x, 0);
        SelectedPositionLine.Visible = (x >= 0 && x <= Size.X);
    }

    public void UpdateSelectedPositionScaling()
    {
        SelectedPositionLine.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
    }

	public void UpdateTimingPointsIndices()
	{
		if (Timing.TimingPoints.Count == 0) return;

		//float startTime = Timing.MusicPositionToTime(MusicPositionStart);

		int firstIndex = Timing.TimingPoints.FindLastIndex(point => point.MusicPosition <= NominalMusicPositionStartForWindow);

        // If there's only TimingPoints AFTER MusicPositionStart
        if (firstIndex == -1) 
            firstIndex = Timing.TimingPoints.FindIndex(point => point.MusicPosition > NominalMusicPositionStartForWindow);

        int lastIndex = Timing.TimingPoints.FindLastIndex(point => point.MusicPosition < NominalMusicPositionStartForWindow + 1);
        if (lastIndex == -1) lastIndex = firstIndex;

		FirstTimingPointIndex = firstIndex;
		LastTimingPointIndex = lastIndex;
    }

    #endregion
    #region Calculators

    public float XPositionToMusicPosition(float x)
	{
        return XPositionToRelativeMusicPosition(x) + NominalMusicPositionStartForWindow;
	}

	/// <summary>
	/// Return music position relative to <see cref="NominalMusicPositionStartForWindow"/>
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	public float XPositionToRelativeMusicPosition(float x)
	{
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + 2 * margin;
		return x * windowLengthInMeasures / Size.X - margin;
    }

	public float MusicPositionToXPosition(float musicPosition)
	{
		float margin = Settings.Instance.MusicPositionMargin;
		float windowLengthInMeasures = 1f + 2 * margin;
		return Size.X * (musicPosition - NominalMusicPositionStartForWindow + margin) / windowLengthInMeasures;
	}

    #endregion
}