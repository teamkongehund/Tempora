using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Parent class for window containing waveform, playhead and timing grid
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
			CreateWaveforms();
		}
	}
	Waveform Waveform1;

	public int MusicPositionStart;

	/// <summary>
	/// List of horizontally stacked waveforms to display
	/// </summary>
    //public List<Waveform> Waveforms = new List<Waveform>();

	public float StartTime = 0;
	public float EndTime = 10;

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
		Playhead.Points = new Vector2[2]
		{
			new Vector2(0, 0),
			new Vector2(0, Size.Y)
		};
		Playhead.Width = 2;
		Playhead.ZIndex = 100;

		//CreateWaveforms();

		//UpdateWaveformAudioFiles();

		Resized += OnResized;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void CreateWaveforms()
	{
        foreach (var child in GetChildren())
        {
            if (child is Waveform waveform)
            {
                waveform.QueueFree();
            }
        }

        Waveform1 = new Waveform(AudioFile, Size.X, Size.Y);
        Waveform1.Position = new Vector2(0, Size.Y / 2);
        Waveform1.TimeRange = new float[2] { StartTime, EndTime };

        AddChild(Waveform1);
        //Waveforms.Add(Waveform1);
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

	public void UpdateVisuals()
	{
        Playhead.Points = new Vector2[2]
        {
            new Vector2(0, 0),
            new Vector2(0, Size.Y)
        };

        foreach (var child in GetChildren())
		{
			if (child is Waveform waveform)
			{
				waveform.Height = Size.Y;
				waveform.Length = Size.X;
				waveform.Position = new Vector2(0, Size.Y / 2);
                waveform.TimeRange = new float[2] { StartTime, EndTime };
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
		UpdateVisuals();
	}
}
