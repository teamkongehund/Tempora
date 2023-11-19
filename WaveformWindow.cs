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

    public Line2D Playhead;
	public Node2D GridFolder;

	private AudioFile _audioFile;
	public AudioFile AudioFile
	{
		get => _audioFile;
		set
		{
			_audioFile = value;
			//UpdateWaveformAudioFiles();
			CreateWaveforms();
            RenderTimingPoints();
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
    #region Godot

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		GridFolder = GetNode<Node2D>("GridFolder");

		Timing = Timing.Instance;

		Playhead = GetNode<Line2D>("Playhead");
        Playhead.Width = 4;
		Playhead.ZIndex = 100;
		UpdatePlayheadScaling();

		Resized += OnResized;

		Signals.Instance.TimingChanged += UpdateTimingPointsIndices;
		Signals.Instance.TimingChanged += CreateWaveforms;
        //Signals.Instance.TimingChanged += RenderOldTimingPoints;
        Signals.Instance.TimingChanged += RenderTimingPoints;
		Signals.Instance.TimingChanged += CreateGridLines;
    }
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                float x = mouseEvent.Position.X;
				float musicPosition = XPositionToMusicPosition(x);
				float time = Timing.MusicPositionToTime(musicPosition);
                GD.Print($"WaveformWindow was clicked at playback time {time} seconds");
				//GD.Print($"BeatPosition = {Timing.GetBeatPosition(musicPosition)}");

                if (Input.IsKeyPressed(Key.Alt))
                {
                    EmitSignal(nameof(SeekPlaybackTime), time);
                }
            }
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && !Input.IsKeyPressed(Key.Alt))
            {
                float x = mouseEvent.Position.X;
                float musicPosition = XPositionToMusicPosition(x);
                float time = Timing.MusicPositionToTime(musicPosition);
                EmitSignal(nameof(DoubleClicked), time);
            }
        }
		else if (@event is InputEventMouseMotion mouseMotion)
		{
			//GD.Print(mouseMotion.Position);

			// If a Timing Point is currently held, get the music position for where the cursor is
			// Change the music position of the timing point to this music position

			TimingPoint timingPoint = Signals.Instance.HeldTimingPoint;
			if (timingPoint == null) return;

			Vector2 mousePos = mouseMotion.Position;

			float musicPosition = XPositionToMusicPosition(mousePos.X);

			timingPoint.MusicPosition = musicPosition;
        }
    }

    #endregion
    #region Render

    public void CreateWaveforms()
	{
		//GD.Print($"{Time.GetTicksMsec()/1e3} - Now rendering waveform window {MusicPositionStart}!");

        foreach (var child in GetChildren())
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

            AddChild(waveform);

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

            AddChild(waveform);
        }
	}

	public void RenderTimingPoints()
	{
        foreach (var child in GetChildren())
        {
            if (child is VisualTimingPoint visualTimingPoint)
                visualTimingPoint.QueueFree();
        }
        foreach (TimingPoint timingPoint in Timing.TimingPoints)
		{
            var packedVisualTimingPoint = ResourceLoader.Load<PackedScene>("res://VisualTimingPoint.tscn");
            if (timingPoint.MusicPosition >= ActualMusicPositionStartForWindow 
				&& timingPoint.MusicPosition < ActualMusicPositionStartForWindow + 1 + 2 * Settings.Instance.MusicPositionMargin)
			{
				VisualTimingPoint visualTimingPoint = packedVisualTimingPoint.Instantiate() as VisualTimingPoint;
				float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);
				visualTimingPoint.TimingPoint = timingPoint;
                visualTimingPoint.Position = new Vector2(x, Size.Y / 2);
				visualTimingPoint.Scale = new Vector2(0.2f, 0.2f);
				AddChild(visualTimingPoint);
			}
		}
	}

	//public void RenderOldTimingPoint(float x)
	//{
	//	Sprite2D sprite = new Sprite2D()
	//	{
	//		Texture = ResourceLoader.Load("res://icon.svg") as Texture2D,
	//		Scale = new Vector2(0.2f, 0.2f),
	//		Position = new Vector2(x, Size.Y/2)
	//	};
	//	AddChild(sprite);
	//}

	//public void RenderOldTimingPoints()
	//{
 //       foreach (var child in GetChildren())
 //       {
 //           if (child is Sprite2D sprite)
	//			sprite.QueueFree();
 //       }

 //       //UpdateTimingPointsIndices();

	//	if (Timing.TimingPoints.Count == 0)
	//	{
	//		return;
	//	}

	//	for (int i = FirstTimingPointIndex; i <= LastTimingPointIndex; i++)
	//	{
	//		TimingPoint timingPoint = Timing.TimingPoints[i];
	//		float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);

	//		if (timingPoint.MusicPosition >= NominalMusicPositionStartForWindow)
	//			RenderOldTimingPoint(x);
	//	}
 //   }

    #endregion
    #region Calculators

    public float XPositionToMusicPosition(float x)
	{
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + 2 * margin;
		return x * windowLengthInMeasures / Size.X - margin + NominalMusicPositionStartForWindow;
	}

	public float MusicPositionToXPosition(float musicPosition)
	{
		float margin = Settings.Instance.MusicPositionMargin;
		float windowLengthInMeasures = 1f + 2 * margin;
		return Size.X * (musicPosition - NominalMusicPositionStartForWindow + margin) / windowLengthInMeasures;
	}
    #endregion
    #region Updaters

    public void OnResized()
    {
		UpdatePlayheadScaling();
        CreateWaveforms();
        //RenderOldTimingPoints();
        RenderTimingPoints();
		CreateGridLines();
    }

	public void UpdatePlayheadScaling()
	{
        Playhead.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
    }
    
	public void CreateGridLines()
	{
		foreach (var child in GridFolder.GetChildren())
		{
			child.QueueFree();
		}

		int divisor = Settings.Instance.Divisor;
        int[] timeSignature = Timing.GetTimeSignature(NominalMusicPositionStartForWindow);

		int index = 0;
		float latestPosition = 0;
		while (latestPosition < 1)
		{
			if (index >= 50) throw new Exception("Too many measure divisions!");

			Line2D gridLine = GetGridLine(timeSignature, divisor, index, out latestPosition);

			if (gridLine == null) break;

			gridLine.ZIndex = 99;

			index++;

			GridFolder.AddChild(gridLine);
		}


		// Based on TimingPoint.TimeSignature and divisor, calculate how many divisons we need, and what their spacing should be



		// Create a Line2D object with appropriate color for each division.

	}

	public Line2D GetGridLine(int[] timeSignature, int divisor, int index)
	{
		float relativePosition;
        Line2D gridLine = GetGridLine(timeSignature, divisor, index, out relativePosition);
		return gridLine;
    }

	public Line2D GetGridLine(int[] timeSignature, int divisor, int index, out float relativePosition)
	{
        relativePosition = Timing.GetRelativeNotePosition(timeSignature, divisor, index);

        if (relativePosition < 0 || relativePosition > 1) return null;

		float margin = Settings.Instance.MusicPositionMargin;
		float xPosition = Size.X * ( (relativePosition + margin) / (2 * margin + 1f));

        Line2D gridLine = new Line2D();
        gridLine.Position = new Vector2(xPosition, 0);
        gridLine.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
		gridLine.DefaultColor = new Color(1f, 0, 0);
		gridLine.Width = 1;

        // TODO: Set color based on divisor and index

        return gridLine;
    }

	// TODO: Redo gridLine.Points via an update method

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

	//public void TrackMouse()
	//{
	//	Vector2 pos = GetGlobalMousePosition();
	//	GD.Print(pos);
	//}

    #endregion
}
