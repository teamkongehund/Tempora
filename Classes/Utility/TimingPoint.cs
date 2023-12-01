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
            MeasuresPerSecond_Update();
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
            MeasuresPerSecond_Update();
        }
    }

	private int[] _timeSignature = new int[] { 4, 4 };
    public int[] TimeSignature
	{
		get => _timeSignature;
		set
		{
			_timeSignature = value;
		}
	}

	private float _time;
	public float Time
	{
		get => _time;
		set
		{
			if (_time == value) return;
			_time = value;
			MeasuresPerSecond_Update();
		}
	}

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

	// TODO 1: Allow easy integer BPM

	public void MeasuresPerSecond_Update()
	{
		if (MusicPosition == null)
			return;
		if (PreviousTimingPoint?.MusicPosition != null)
		{
			PreviousTimingPoint.MeasuresPerSecond =
				((float)MusicPosition - (float)PreviousTimingPoint.MusicPosition)
				/ (Time - PreviousTimingPoint.Time);
			MeasuresPerSecond = PreviousTimingPoint.MeasuresPerSecond;
		}
		if (NextTimingPoint?.MusicPosition != null)
		{
			MeasuresPerSecond =
				((float)NextTimingPoint.MusicPosition - (float)MusicPosition)
				/ (NextTimingPoint.Time - Time);
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
		private set
		{
			if (_bpm == value) return;
			_bpm = value;
		}
	}
	public void BPM_Update()
	{
		BPM = MeasuresPerSecond * 60 * (TimeSignature[0] * 4f / TimeSignature[1]);
	}

	public void BPM_Update(float bpm)
	{
		if (NextTimingPoint != null)
			return;
		BPM = bpm;
		MeasuresPerSecond = BPM / (60 * (TimeSignature[0] * 4f / TimeSignature[1]));
		EmitSignal(nameof(Changed), this);
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

            _musicPosition = value;

            // Update MPS for this timing point and the previous one
			MeasuresPerSecond_Update();

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
