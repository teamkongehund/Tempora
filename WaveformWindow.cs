using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Parent class for window containing waveform(s), playhead and timing grid
/// </summary>
public partial class WaveformWindow : Control
{
    [Signal] public delegate void SeekPlaybackTimeEventHandler(float playbackTime);
	[Signal] public delegate void AddTimingPointEventHandler(float playbackTime);

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
			RenderWaveforms();
		}
	}
	Waveform Waveform1;

	private int _musicPositionStart;
	public int MusicPositionStart
	{
		get => _musicPositionStart;
		set
		{
			_musicPositionStart = value;
			RenderWaveforms();
		}
	}

	/// <summary>
	/// List of horizontally stacked waveforms to display
	/// </summary>
    //public List<Waveform> Waveforms = new List<Waveform>();

	//public float StartTime = 0;
	//public float EndTime = 10;

	Timing Timing = Timing.Instance;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		//var children = GetChildren();
		//foreach (var child in children)
		//{
		//	GD.Print(child);
		//}
		//GD.Print("Suh dude");

		GridFolder = GetNode<Node>("GridFolder");

		Playhead = GetNode<Line2D>("Playhead");
        Playhead.Width = 2;
		Playhead.ZIndex = 100;
		UpdatePlayheadScaling();

		//CreateWaveforms();

		//UpdateWaveformAudioFiles();

		Resized += OnResized;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void RenderWaveforms()
	{
		GD.Print($"{Time.GetTicksMsec()/1e3} - Now rendering waveforms!");

        foreach (var child in GetChildren())
        {
            if (child is Waveform waveform)
            {
                waveform.QueueFree();
            }
        }

		// Old algorithm with single waveform:
        //Waveform1 = new Waveform(AudioFile, Size.X, Size.Y);
        //Waveform1.Position = new Vector2(0, Size.Y / 2);
        //Waveform1.TimeRange = new float[2] { StartTime, EndTime };

        //AddChild(Waveform1);



		// New algorithm with multiple waveforms:

		if (Timing.TimingPoints.Count == 0)
		{
			float startTime = (float)MusicPositionStart / 0.5f;
			float endTime = (float)(MusicPositionStart+1) / 0.5f;

            Waveform1 = new Waveform(AudioFile, Size.X, Size.Y);
            Waveform1.Position = new Vector2(0, Size.Y / 2);
            Waveform1.TimeRange = new float[2] { startTime, endTime };

            AddChild(Waveform1);

			GD.Print($"There were no timing points... Using MusicPositionStart {MusicPositionStart} to set start and end times.");

			return;
        }

		int indexForFirstTimingPointToUse = Timing.TimingPoints.FindLastIndex(point => point.MusicPosition <= MusicPositionStart);

		if (indexForFirstTimingPointToUse == -1) // If there's only TimingPoints AFTER MusicPositionStart
		{
            indexForFirstTimingPointToUse = Timing.TimingPoints.FindIndex(point => point.MusicPosition > MusicPositionStart);
			
			//float startTime = (float)MusicPositionStart / Timing.TimingPoints[indexForFirstTimingPointToUse].MeasuresPerSecond;
			//float endTime = (float)(MusicPositionStart+1) / Timing.TimingPoints[indexForFirstTimingPointToUse].MeasuresPerSecond;
        }

		int indexForLastTimingPointToUse = Timing.TimingPoints.FindLastIndex(point => point.MusicPosition < MusicPositionStart+1);
		if (indexForLastTimingPointToUse == -1) indexForLastTimingPointToUse = indexForFirstTimingPointToUse;

		//GD.Print($"Rendering waveforms - Using TimingPoints indices {indexForFirstTimingPointToUse} .. {indexForLastTimingPointToUse}");

		TimingPoint firstTimingPoint = Timing.TimingPoints[indexForFirstTimingPointToUse];
        TimingPoint lastTimingPoint = Timing.TimingPoints[indexForLastTimingPointToUse];

        float timeWhereWindowBegins = firstTimingPoint.Time + (MusicPositionStart - (float)firstTimingPoint.MusicPosition) / firstTimingPoint.MeasuresPerSecond;
		float timeWhereWindowEnds = lastTimingPoint.Time + (MusicPositionStart + 1 - (float)lastTimingPoint.MusicPosition) / lastTimingPoint.MeasuresPerSecond;

		// Create each waveform segment
        for (int i = indexForFirstTimingPointToUse; i < indexForLastTimingPointToUse+1; i++) 
		{
			//GD.Print($"Now rendering waveform for index {i}");
			TimingPoint timingPoint = Timing.TimingPoints[i];

			float startTime = (i == indexForFirstTimingPointToUse)
				? timeWhereWindowBegins
                : (float)timingPoint.Time;

			float endTime = (i == indexForLastTimingPointToUse)
				? timeWhereWindowEnds
                : (float)Timing.TimingPoints[i+1].Time;

			float length = Size.X * (endTime - startTime) / (timeWhereWindowEnds - timeWhereWindowBegins);
			float xPosition = Size.X * (startTime - timeWhereWindowBegins) / (timeWhereWindowEnds - timeWhereWindowBegins);

			Waveform waveform = new Waveform(AudioFile, length, Size.Y);
            waveform.Position = new Vector2(xPosition, Size.Y / 2);
            waveform.TimeRange = new float[2] { startTime, endTime };
			
			// TODO: Make random colors so it's easy to see what's going on.
			//waveform.DefaultColor = new Color()
            AddChild(waveform);
        }

		GD.Print($"{Time.GetTicksMsec()/1e3} - Done rendering waveforms!");

	}

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                float x = mouseEvent.Position.X;
                float clickedPlaybackTime = Waveform1.PixelPositionToPlaybackTime(x);
                GD.Print($"WaveformWindow was clicked at playback time {clickedPlaybackTime} seconds");

                if (Input.IsKeyPressed(Key.Alt))
				{
                    EmitSignal(nameof(SeekPlaybackTime), clickedPlaybackTime);
                }
            }
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && !Input.IsKeyPressed(Key.Alt))
			{
                GD.Print($"WaveformWindow - User double clicked on {mouseEvent.Position}!");
                float x = mouseEvent.Position.X;
                float clickedPlaybackTime = Waveform1.PixelPositionToPlaybackTime(x);
				EmitSignal(nameof(AddTimingPoint), clickedPlaybackTime);
			}
        }
    }

	public void UpdateScaling()
	{
		UpdatePlayheadScaling();
		UpdateWaveformScaling();

		//foreach (var child in GetChildren())
		//{
		//	if (child is Waveform waveform)
		//	{
		//		waveform.TimeRange = new float[2] { StartTime, EndTime };
		//	}
		//}
	}

	public void UpdatePlayheadScaling()
	{
        Playhead.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };
    }

	public void UpdateWaveformScaling()
	{
        foreach (var child in GetChildren())
        {
            if (child is Waveform waveform)
            {
                waveform.Height = Size.Y;
                waveform.Length = Size.X;
                waveform.Position = new Vector2(waveform.Position.X, Size.Y / 2);
            }
        }
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

	public void OnResized()
	{
		UpdateScaling();
	}
}
