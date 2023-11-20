using Godot;
using System;

public partial class TimingPoint : Node , IComparable<TimingPoint>
{
	[Signal] public delegate void ChangedEventHandler(TimingPoint timingPoint);
    [Signal] public delegate void DeletedEventHandler(TimingPoint timingPoint);

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
			if (_musicPosition != value)
			{
				_musicPosition = value;
				EmitSignal(nameof(Changed), this); // Should happen when user drags timing point to new position
			}
		}
	}

	/// <summary>
	/// Relies on parent <see cref="Timing"/> to delete from project.
	/// </summary>
	public void Delete()
	{
		EmitSignal(nameof(Deleted), this);
	}

    public int CompareTo(TimingPoint other) => Time.CompareTo(other.Time);
}
