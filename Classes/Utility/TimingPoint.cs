using Godot;
using System;

public partial class TimingPoint : Node , IComparable<TimingPoint>
{
	[Signal] public delegate void ChangedEventHandler(TimingPoint timingPoint);
    [Signal] public delegate void DeletedEventHandler(TimingPoint timingPoint);

	private TimingPoint _previousTimingPoint;
	public TimingPoint PreviousTimingPoint
	{
		get => _previousTimingPoint;
        set
		{
			if (_previousTimingPoint == value) return;
			_previousTimingPoint = value;
			if (PreviousTimingPoint != null) PreviousTimingPoint.NextTimingPoint = this;
		}
	}
    private TimingPoint _nextTimingPoint;
    public TimingPoint NextTimingPoint
    {
        get => _nextTimingPoint;
        set
        {
            if (_nextTimingPoint == value) return;
            _nextTimingPoint = value;
            if (NextTimingPoint != null) NextTimingPoint.PreviousTimingPoint = this;
        }
    }

    public int[] TimeSignature = new int[] { 4, 4 };

	public float Time;

	/// <summary>
	/// The tempo from this timing point until the next. The value is proportional to BPM if the time signature doesn't change.
	/// </summary>
	private float _measuresPerSecond = 0.5f;
	public float MeasuresPerSecond
    {
        get => _measuresPerSecond;
        set
        {
			if (_measuresPerSecond != value)
			{
				_measuresPerSecond = value;
				BPM_Update();
			}
        }
    }

	private float _bpm;
	public float BPM
	{
		get
		{
			if (_bpm == 0) BPM_Update();
			return _bpm;
		}
		private set { }
	}
	public void BPM_Update()
	{
		_bpm = MeasuresPerSecond * 60 * (TimeSignature[0] * 4f / TimeSignature[1]);
	}

	public float BeatLength
	{
		get
		{
			return 1 / (BPM / 60);
		}
	}

	private float? _musicPosition;
	public float? MusicPosition
	{
		get 
		{ 
			return _musicPosition; 
		}
		set
		{
			if (_musicPosition == value) return;
			if (PreviousTimingPoint != null && PreviousTimingPoint.MusicPosition >= value) return;
            if (NextTimingPoint != null && NextTimingPoint.MusicPosition <= value) return;

			//GD.Print($"{PreviousTimingPoint.MusicPosition} , {PreviousTimingPoint.MusicPosition} , snapping to {value}");

            _musicPosition = value;
			EmitSignal(nameof(Changed), this);
		}
	}

	/// <summary>
	/// Relies on parent <see cref="Timing"/> to delete from project.
	/// </summary>
	public void Delete()
	{
        if (PreviousTimingPoint != null) PreviousTimingPoint.NextTimingPoint = NextTimingPoint;
        if (NextTimingPoint != null) NextTimingPoint.PreviousTimingPoint = PreviousTimingPoint;
        EmitSignal(nameof(Deleted), this);
	}

    public int CompareTo(TimingPoint other) => Time.CompareTo(other.Time);
}