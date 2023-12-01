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
	[Signal] public delegate void AttemptToAddTimingPointEventHandler(float playbackTime);

    public Node2D WaveformFolder;
	public Node2D VisualTimingPointFolder;
    public Line2D Playhead;
	public Node2D GridFolder;
	public Line2D PreviewLine;
    public Line2D SelectedPositionLine;
    public Label MeasureLabel;
    public TimeSignatureLineEdit TimeSignatureLineEdit;

    PackedScene PackedVisualTimingPoint = ResourceLoader.Load<PackedScene>("res://Classes/Visual/VisualTimingPoint.tscn");

    public bool IsInstantiating = true;

 //   private AudioFile _audioFile;
	//public AudioFile AudioFile
	//{
	//	get => _audioFile;
	//	set
	//	{
	//		if (_audioFile != value)
	//		{
	//			_audioFile = value;
	//			//UpdateWaveformAudioFiles();
	//			CreateWaveforms();
	//			RenderTimingPoints();
	//		}
 //       }
	//}
	//WaveformLine2D Waveform1;

	private int _musicPositionStart;
	public int NominalMusicPositionStartForWindow
	{
		get => _musicPositionStart;
		set
		{
            if (_musicPositionStart == value) return;
			_musicPositionStart = value;

            if (!IsInstantiating)
                UpdateVisuals();
        }
	}

	public float ActualMusicPositionStartForWindow
    {
        get => NominalMusicPositionStartForWindow - Settings.Instance.MusicPositionMargin - Settings.Instance.MusicPositionOffset;
        private set { }
    }

    /// <summary>
    /// List of horizontally stacked waveforms to display
    /// </summary>
    //public List<Waveform> Waveforms = new List<Waveform>();

    //public float StartTime = 0;
    //public float EndTime = 10;

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
		
		Playhead = GetNode<Line2D>("Playhead");
        Playhead.Width = 4;
		Playhead.ZIndex = 100;
		UpdatePlayheadScaling();
		UpdatePreviewLineScaling();
        UpdateSelectedPositionScaling();
        UpdateSelectedPositionLine();

        CreateEmptyTimingPoints(8);

        Resized += OnResized;
		//Signals.Instance.SettingsChanged += OnSettingsChanged;
  //      Signals.Instance.SelectedPositionChanged += OnSelectedPositionChanged;
  //      Signals.Instance.Scrolled += UpdateSelectedPositionLine;
        Signals.Instance.Connect("SettingsChanged", Callable.From(OnSettingsChanged));
        Signals.Instance.Connect("SelectedPositionChanged", Callable.From(OnSelectedPositionChanged));
        Signals.Instance.Connect("Scrolled", Callable.From(UpdateSelectedPositionLine));

        Signals.Instance.AudioFileChanged += OnAudioFileChanged;

        //TimeSignatureLineEdit.TimeSignatureSubmitted += OnTimingSignatureSubmitted;
        TimeSignatureLineEdit.Connect("TimeSignatureSubmitted", new Callable(this, "OnTimingSignatureSubmitted"));

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
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                float x = mouseEvent.Position.X;
				float musicPosition = XPositionToMusicPosition(x);
				float time = Timing.Instance.MusicPositionToTime(musicPosition);
                GD.Print($"WaveformWindow was clicked at playback time {time} seconds");

                if (Input.IsKeyPressed(Key.Alt))
                {
                    Context.Instance.IsSelectedMusicPositionMoving = true;
                    Context.Instance.SelectedMusicPosition = XPositionToMusicPosition(x);
                }
                else
                {
                    x = mouseEvent.Position.X;
                    musicPosition = XPositionToMusicPosition(x);
                    time = Timing.Instance.MusicPositionToTime(musicPosition);
                    EmitSignal(nameof(AttemptToAddTimingPoint), time);
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                float x = mouseEvent.Position.X;
                float musicPosition = XPositionToMusicPosition(x);
                float time = Timing.Instance.MusicPositionToTime(musicPosition);
                EmitSignal(nameof(SeekPlaybackTime), time);
            }
        }
		else if (@event is InputEventMouseMotion mouseMotion)
		{
            Vector2 mousePos = mouseMotion.Position;
            float mouseMusicPosition = XPositionToMusicPosition(mousePos.X);
            float mouseRelativeMusicPosition = XPositionToRelativeMusicPosition(mousePos.X);
;
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
        if (!Visible) return;
		UpdateTimingPointsIndices();
		CreateWaveforms();
		RenderTimingPoints();
		CreateGridLines();
        UpdateLabels();
    }

    public void OnAudioFileChanged()
    {
        CreateWaveforms();
    }

    public void OnTimingSignatureSubmitted(int[] timeSignature)
    {
        Timing.Instance.UpdateTimeSignature(timeSignature, NominalMusicPositionStartForWindow);
    }
	public void OnResized()
    {
        //GD.Print("Resized!");
        UpdateVisuals();
    }

    public void OnSettingsChanged() => UpdateVisuals();

    public void OnSelectedPositionChanged() => UpdateSelectedPositionLine();

    public void OnMouseEntered() => PreviewLine.Visible = true;

    public void OnMouseExited() => PreviewLine.Visible = false;

    #endregion
    #region Render

    // TODO 2: Add 20 ms grid line window delimeters (or make width of grid lines 20 ms)

    public void CreateWaveforms()
	{
        // TODO 1: Re-do waveform rendering as a shader or piece of code that takes a section of a pre-rendering
        // image file. This should significantly reduce CPU usage.
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

        float timeWhereWindowBegins = Timing.Instance.MusicPositionToTime(ActualMusicPositionStartForWindow);
        float timeWhereWindowEnds = Timing.Instance.MusicPositionToTime(ActualMusicPositionStartForWindow + 1 + 2 * margin);

        if (Timing.Instance.TimingPoints.Count == 0)
		{
			float startTime = timeWhereWindowBegins;
			float endTime = timeWhereWindowEnds;

            Waveform waveform = new Waveform(Project.Instance.AudioFile, Size.X, Size.Y, new float[2] { startTime, endTime })
            {
                Position = new Vector2(0, Size.Y / 2),
            };

            WaveformFolder.AddChild(waveform);

			//GD.Print($"There were no timing points... Using MusicPositionStart {MusicPositionStartForWindow} to set start and end times.");

			return;
        }

        //GD.Print($"Window {MusicPositionStart}: timeWhereWindowBeginds = {timeWhereWindowBegins} , timeWhereWindowEnds = {timeWhereWindowEnds}");

        UpdateTimingPointsIndices();

        // Create each waveform segment
        for (int i = FirstTimingPointIndex; i <= LastTimingPointIndex; i++) 
		{
			//GD.Print($"Now rendering waveform for index {i}");
			TimingPoint timingPoint = Timing.Instance.TimingPoints[i];

			float waveSegmentStartTime = (i == FirstTimingPointIndex)
				? timeWhereWindowBegins
                : (float)timingPoint.Time;

			float waveSegmentEndTime = (i == LastTimingPointIndex)
				? timeWhereWindowEnds
                : (float)Timing.Instance.TimingPoints[i+1].Time;

            //GD.Print($"Timing Points {FirstTimingPointIndex} - {LastTimingPointIndex}: Time {waveSegmentStartTime} - {waveSegmentEndTime}");

            float musicPositionStart = Timing.Instance.TimeToMusicPosition(waveSegmentStartTime);
			float musicPositionEnd = Timing.Instance.TimeToMusicPosition(waveSegmentEndTime);

            float length = Size.X * (musicPositionEnd - musicPositionStart) / (1f + 2 * margin);
            float xPosition = Size.X * (musicPositionStart - ActualMusicPositionStartForWindow) / (1f + 2 * margin);

            Waveform waveform = new Waveform(Project.Instance.AudioFile, length, Size.Y, new float[2] { waveSegmentStartTime, waveSegmentEndTime })
			{
				Position = new Vector2(xPosition, Size.Y / 2),
			};

            // Randomize color, so it's easy to see what's happening
            //Random random = new Random();
            //waveform.DefaultColor = new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1);

            // Note: this one also takes some processing
            WaveformFolder.AddChild(waveform);
        }
	}

    /// <summary>
    /// Instantiate an amount of <see cref="VisualTimingPoint"/> nodes and adds as children. 
    /// These are then modified whenever the visuals need to change, instead of re-instantiating them.
    /// This is significantly faster than creating them anew each time.
    /// </summary>
    public void CreateEmptyTimingPoints(int amount)
    {
        foreach (var child in VisualTimingPointFolder.GetChildren())
        {
            if (child is VisualTimingPoint visualTimingPoint)
                visualTimingPoint.QueueFree();
        }

        TimingPoint dummyTimingPoint = new TimingPoint()
        {
            Time = 0f,
            MusicPosition = 0f,
            MeasuresPerSecond = 120
        };

        for (int i = 0; i < amount; i++)
        {
            VisualTimingPoint visualTimingPoint;
            AddVisualTimingPoint(dummyTimingPoint, out visualTimingPoint);
        }
    }

    public void AddVisualTimingPoint(TimingPoint timingPoint, out VisualTimingPoint visualTimingPoint)
    {
        visualTimingPoint = PackedVisualTimingPoint.Instantiate() as VisualTimingPoint;
        visualTimingPoint.Visible = false;
        visualTimingPoint.Scale = new Vector2(0.2f, 0.2f);
        visualTimingPoint.ZIndex = 95;
        visualTimingPoint.TimingPoint = timingPoint;
        VisualTimingPointFolder.AddChild(visualTimingPoint);
    }

    public void RenderTimingPoints()
    {
        Godot.Collections.Array<Node> children = VisualTimingPointFolder.GetChildren();
        int childrenAmount = children.Count;
        int index = 0;

        foreach (Node2D child in children)
        {
            child.Visible = false;
        }

        // this assumes all children are VisualTimingPoint
        foreach (TimingPoint timingPoint in Timing.Instance.TimingPoints)
        {
            if (timingPoint.MusicPosition < ActualMusicPositionStartForWindow
               || timingPoint.MusicPosition > (ActualMusicPositionStartForWindow + 1 + 2 * Settings.Instance.MusicPositionMargin))
                continue;

            VisualTimingPoint visualTimingPoint;
            if (index >= childrenAmount)
            {
                AddVisualTimingPoint(timingPoint, out visualTimingPoint);
            }
            else
            {
                visualTimingPoint = children[index] as VisualTimingPoint;
            }
            float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);
            visualTimingPoint.TimingPoint = timingPoint;
            visualTimingPoint.Position = new Vector2(x, Size.Y / 2);
            visualTimingPoint.UpdateLabels(timingPoint);
            visualTimingPoint.Visible = true;
            index++;
        }
    }
	public void CreateGridLines()
	{
		foreach (GridLine child in GridFolder.GetChildren())
		{
			child.QueueFree();
		}

		int divisor = Settings.Instance.Divisor;
        int[] timeSignaturePrevious = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow-1);
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow);

		int divisionIndex = 0;
		float latestPosition = 0;
        // Render GridLines from previous measure
        while (latestPosition < 1)
        {
            if (divisionIndex >= 50) throw new Exception("Too many measure divisions!");

            GridLine gridLine = GetGridLine(timeSignaturePrevious, divisor, divisionIndex, -1);

            if (gridLine == null || gridLine.RelativeMusicPosition >= 1) break;

            divisionIndex++;
            latestPosition = gridLine.RelativeMusicPosition;

            float musicPosition = gridLine.RelativeMusicPosition + NominalMusicPositionStartForWindow - 1;
            if (musicPosition < ActualMusicPositionStartForWindow) continue;

            GridFolder.AddChild(gridLine);
        }
        divisionIndex = 0;
        latestPosition = 0; // TODO 1: finish this offset thingy
        while (latestPosition < 1)
		{
			if (divisionIndex >= 50) throw new Exception("Too many measure divisions!");

			GridLine gridLine = GetGridLine(timeSignature, divisor, divisionIndex, 0);

			if (gridLine == null) break;

            divisionIndex++;
            latestPosition = gridLine.RelativeMusicPosition;

            float musicPosition = gridLine.RelativeMusicPosition + NominalMusicPositionStartForWindow;
            if (musicPosition > ActualMusicPositionStartForWindow + 1 + 2 * Settings.Instance.MusicPositionMargin) break;



            // TODO 2: Pre-instantiatie grid lines and toggle visibility instead.
            // AddChild uses a lot of CPU usage.
            // Further, GetGridLine also uses a bit because of constant instantiation.
            // Again, if you pre-instantiate and change position, visibility and color, etc, this is more efficient.
			GridFolder.AddChild(gridLine);
		}
	}
	public GridLine GetGridLine(int[] timeSignature, int divisor, int index, int measureOffset)
	{
		GridLine gridLine = new GridLine(timeSignature, divisor, index);

        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float xPosition = Size.X * ((gridLine.RelativeMusicPosition + measureOffset + margin + offset) / (2 * margin + 1f));
        gridLine.Position = new Vector2(xPosition, 0);
        gridLine.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
        gridLine.ZIndex = 90;

        return gridLine;
	}

    #endregion
    #region Updaters
    public void UpdateVisuals()
    {
        if (!Visible) return;
        UpdatePlayheadScaling();
        UpdatePreviewLineScaling();
        UpdateSelectedPositionScaling();
        CreateWaveforms(); // takes 2-5 ms on 30 blocks  loaded
        RenderTimingPoints(); // takes 2-3 ms on 30 blocks loaded
        CreateGridLines();
        UpdateLabels();
    }

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow);
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
		if (Timing.Instance.TimingPoints.Count == 0) return;

        float margin = Settings.Instance.MusicPositionMargin;

        int firstIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MusicPosition <= ActualMusicPositionStartForWindow);
        // If there's only TimingPoints AFTER MusicPositionStart
        if (firstIndex == -1)
            firstIndex = Timing.Instance.TimingPoints.FindIndex(point => point.MusicPosition > ActualMusicPositionStartForWindow);
        int lastIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MusicPosition < ActualMusicPositionStartForWindow + 1 + 2 * margin);
        if (lastIndex == -1) lastIndex = firstIndex;

        FirstTimingPointIndex = firstIndex;
		LastTimingPointIndex = lastIndex;
    }

    #endregion
    #region Calculators

    public float XPositionToMusicPosition(float x)
	{
        //GD.Print($"XPositionToMusicPosition: NominalMusicPositionStartForWindow = {NominalMusicPositionStartForWindow}");
        return XPositionToRelativeMusicPosition(x) + NominalMusicPositionStartForWindow;
	}

	/// <summary>
	/// Return music position relative to <see cref="NominalMusicPositionStartForWindow"/>
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	public float XPositionToRelativeMusicPosition(float x)
	{
        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + 2 * margin;
		return x * windowLengthInMeasures / Size.X - margin - offset;
    }

	public float MusicPositionToXPosition(float musicPosition)
	{
        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
		float windowLengthInMeasures = 1f + 2 * margin;
		return Size.X * (musicPosition - NominalMusicPositionStartForWindow + margin + offset) / windowLengthInMeasures;
	}

    #endregion
}
