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
	public Node GridFolder;

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
	public int MusicPositionStartForWindow
	{
		get => _musicPositionStart;
		set
		{
			_musicPositionStart = value;
			CreateWaveforms();
            RenderTimingPoints();
        }
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
		GridFolder = GetNode<Node>("GridFolder");

		Timing = Timing.Instance;

		Playhead = GetNode<Line2D>("Playhead");
        Playhead.Width = 2;
		Playhead.ZIndex = 100;
		UpdatePlayheadScaling();

		Resized += OnResized;

		Signals.Instance.TimingChanged += UpdateTimingPointsIndices;
		Signals.Instance.TimingChanged += CreateWaveforms;
        //Signals.Instance.TimingChanged += RenderOldTimingPoints;
        Signals.Instance.TimingChanged += RenderTimingPoints;
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
			if (child is Sprite2D sprite)
			{
				sprite.QueueFree();
			}
        }

        float timeWhereWindowBegins = Timing.MusicPositionToTime(MusicPositionStartForWindow);
        float timeWhereWindowEnds = Timing.MusicPositionToTime(MusicPositionStartForWindow + 1);

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

            float length = Size.X * (musicPositionEnd - musicPositionStart) / 1f;
            float xPosition = Size.X * (musicPositionStart - MusicPositionStartForWindow) / 1f;

            Waveform waveform = new Waveform(AudioFile, length, Size.Y, new float[2] { startTime, endTime })
			{
				Position = new Vector2(xPosition, Size.Y / 2),
			};
			
			Random random = new Random();

			// Randomize color, so it's easy to see what's happening
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
            if (timingPoint.MusicPosition >= MusicPositionStartForWindow && timingPoint.MusicPosition < MusicPositionStartForWindow + 1)
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

	public void RenderOldTimingPoint(float x)
	{
		Sprite2D sprite = new Sprite2D()
		{
			Texture = ResourceLoader.Load("res://icon.svg") as Texture2D,
			Scale = new Vector2(0.2f, 0.2f),
			Position = new Vector2(x, Size.Y/2)
		};
		AddChild(sprite);
	}

	public void RenderOldTimingPoints()
	{
        foreach (var child in GetChildren())
        {
            if (child is Sprite2D sprite)
				sprite.QueueFree();
        }

        //UpdateTimingPointsIndices();

		if (Timing.TimingPoints.Count == 0)
		{
			return;
		}

		for (int i = FirstTimingPointIndex; i <= LastTimingPointIndex; i++)
		{
			TimingPoint timingPoint = Timing.TimingPoints[i];
			float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);

			if (timingPoint.MusicPosition >= MusicPositionStartForWindow)
				RenderOldTimingPoint(x);
		}
    }

    #endregion
    #region Calculators

    public float XPositionToMusicPosition(float x)
	{
		return MusicPositionStartForWindow + x / Size.X;
	}

	public float MusicPositionToXPosition(float musicPosition)
	{
		return Size.X * (musicPosition - MusicPositionStartForWindow) / 1f;
	}
    #endregion
    #region Updaters

    public void OnResized()
    {
		UpdatePlayheadScaling();
        CreateWaveforms();
        //RenderOldTimingPoints();
        RenderTimingPoints();
    }

	public void UpdatePlayheadScaling()
	{
        Playhead.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
    }
    
	public void UpdateGrid()
	{
		foreach (var child in GridFolder.GetChildren())
		{
			child.QueueFree();
		}

		int divisor = Settings.Instance.Divisor;

		// Find TimingPoiont that defines this measure

		// Based on TimingPoint.TimeSignature and divisor, calculate how many divisons we need, and what their spacing should be



		// Create a Line2D object with appropriate color for each division.

	}

	public void UpdateTimingPointsIndices()
	{
		if (Timing.TimingPoints.Count == 0) return;

		//float startTime = Timing.MusicPositionToTime(MusicPositionStart);

		int firstIndex = Timing.TimingPoints.FindLastIndex(point => point.MusicPosition <= MusicPositionStartForWindow);

        // If there's only TimingPoints AFTER MusicPositionStart
        if (firstIndex == -1) 
            firstIndex = Timing.TimingPoints.FindIndex(point => point.MusicPosition > MusicPositionStartForWindow);

        int lastIndex = Timing.TimingPoints.FindLastIndex(point => point.MusicPosition < MusicPositionStartForWindow + 1);
        if (lastIndex == -1) lastIndex = firstIndex;

		FirstTimingPointIndex = firstIndex;
		LastTimingPointIndex = lastIndex;
    }

	public void TrackMouse()
	{
		Vector2 pos = GetGlobalMousePosition();
		GD.Print(pos);
	}

    #endregion
}
