using Godot;
using System;

public partial class TimingPoint : Node , IComparable<TimingPoint>
{
	[Signal] public delegate void TimingPointChangedEventHandler(TimingPoint timingPoint);

	public int[] TimeSignature = new int[] { 4, 4 };

	public float Time;

	public float MeasuresPerSecond = 0.5f;

	public float BPM
	{
		get
		{
			return MeasuresPerSecond * 60 * (TimeSignature[0] * 4f/TimeSignature[1]);
		}
		private set { }
	}

	private float _musicPosition;
	public float MusicPosition
	{
		get { return _musicPosition; }
		set
		{
			if (_musicPosition != value)
			{
				_musicPosition = value;
				EmitSignal(nameof(TimingPointChanged), this);
			}
		}
	}

    public int CompareTo(TimingPoint other) => Time.CompareTo(other.Time);
}
