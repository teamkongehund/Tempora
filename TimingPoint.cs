using Godot;
using System;

public partial class TimingPoint : Node , IComparable<TimingPoint>
{
	[Signal] public delegate void TimingPointChangedEventHandler(TimingPoint timingPoint);

	public int[] TimeSignature = new int[] { 4, 4 };

	public float Time;

	/// <summary>
	/// The tempo from this timing point until the next. The value is proportional to BPM if the time signature doesn't change.
	/// </summary>
	public float MeasuresPerSecond = 0.5f;

	public float BPM
	{
		get
		{
			return MeasuresPerSecond * 60 * (TimeSignature[0] * 4f/TimeSignature[1]);
		}
		private set { }
	}

	private float? _musicPosition;
	public float? MusicPosition
	{
		get 
		{ 
			if (_musicPosition == null) 
			{
				throw new Exception("MusicPosition.get -> MusicPosition was null");	
			}
			return _musicPosition; 
		}
		set
		{
			if (_musicPosition != value)
			{
				_musicPosition = value;
				EmitSignal(nameof(TimingPointChanged), this); // Should happen when user drags timing point to new position
			}
		}
	}

    public int CompareTo(TimingPoint other) => Time.CompareTo(other.Time);
}
