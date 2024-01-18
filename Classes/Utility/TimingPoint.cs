using System;
using Godot;

namespace OsuTimer.Classes.Utility;

public partial class TimingPoint : Node, IComparable<TimingPoint>, ICloneable
{
    [Signal]
    public delegate void ChangedEventHandler(TimingPoint timingPoint);

    // TODO: Replace all ".Changed += ..." with this event instead.
    public event EventHandler ChangeFinalized = null!;

    [Signal]
    public delegate void DeletedEventHandler(TimingPoint timingPoint);

    /// <summary>
    ///     The tempo from this timing point until the next. The value is proportional to BPM if the time signature doesn't
    ///     change.
    /// </summary>
    private float measuresPerSecond = 0.5f;

    private int[] timeSignature = { 4, 4 };
    //private TimingPoint nextTimingPoint;

    //private TimingPoint previousTimingPoint;

    //public TimingPoint PreviousTimingPoint {
    //    //get => previousTimingPoint;
    //    get
    //    {
    //        // Timing.Instance.GetPreviousTimingPoint(this) != previousTimingPoint
    //        if (previousTimingPoint != null)
    //        {

    //        }
    //        return previousTimingPoint;
    //    }
    //    set {
    //        if (previousTimingPoint == value) return;
    //        previousTimingPoint = value;
    //        if (previousTimingPoint != null) previousTimingPoint.NextTimingPoint = this;
    //        MeasuresPerSecond_Update();
    //    }
    //}

    //public TimingPoint NextTimingPoint {
    //    //get => nextTimingPoint;
    //    get
    //    {
    //        // Timing.Instance.GetNextTimingPoint(this) != nextTimingPoint
    //        if (nextTimingPoint != null)
    //        {

    //        }
    //        return nextTimingPoint;
    //    }
    //    set {
    //        if (nextTimingPoint == value) return;
    //        nextTimingPoint = value;
    //        if (nextTimingPoint != null) nextTimingPoint.PreviousTimingPoint = this;
    //        MeasuresPerSecond_Update();
    //    }
    //}

    public int[] TimeSignature
    {
        get => timeSignature;
        set
        {
            timeSignature = value;
            MeasuresPerSecond_Update();
        }
    }

    private float time;
    public float NewTime;
    public bool IsNewTimeValid = false;
    public event EventHandler TimeChanged = null!;

    public float Time
    {
        get => time;
        set
        {
            if (time == value)
                return;
            //if (PreviousTimingPoint != null && PreviousTimingPoint.Time >= value) return;
            //if (NextTimingPoint != null && NextTimingPoint.Time <= value) return;

            if (IsNewTimeValid)
            {
                time = value;
                IsNewTimeValid = false;
                MeasuresPerSecond_Update();
                return;
            }

            NewTime = value;

            TimeChanged?.Invoke(this, EventArgs.Empty); // Received by Timing class

            //time = value;
            //MeasuresPerSecond_Update();

            //EmitSignal(nameof(Changed), this);
        }
    }

    public float MeasuresPerSecond
    {
        get => measuresPerSecond;
        set
        {
            if (measuresPerSecond != value)
            {
                measuresPerSecond = value;
                Bpm = MpsToBpm(value);
                _ = EmitSignal(nameof(Changed), this);
            }
        }
    }

    private float bpm;
    public float NewBpm;
    public bool IsNewBpmValid = false;
    public event EventHandler BpmChanged = null!;
    public float Bpm
    {
        get
        {
            if (bpm == 0)
                Bpm = MpsToBpm(MeasuresPerSecond);
            return bpm;
        }
        set
        {
            if (bpm == value)
                return;
            //if (NextTimingPoint != null)
            //    return;
            //bpm = value;
            //MeasuresPerSecond = BpmToMps(bpm);

            if (IsNewBpmValid)
            {
                bpm = value;
                IsNewBpmValid = false;
                MeasuresPerSecond = BpmToMps(bpm);
                return;
            }

            NewBpm = value;

            BpmChanged?.Invoke(this, EventArgs.Empty); // Received by Timing class
        }
    }

    private float BpmToMps(float bpm) => bpm / (60 * (TimeSignature[0] * 4f / TimeSignature[1]));

    private float MpsToBpm(float mps) => mps * 60 * (TimeSignature[0] * 4f / TimeSignature[1]);

    public float BeatLengthSec => 1 / (Bpm / 60);

    private float? musicPosition;
    public float? NewMusicPosition;
    public bool IsNewMusicPositionValid = false;
    public event EventHandler MusicPositionChanged = null!;
    public float? MusicPosition
    {
        get => musicPosition;
        set
        {
            if (musicPosition == value)
                return;
            //if (PreviousTimingPoint != null && PreviousTimingPoint.MusicPosition >= value) return;
            //if (NextTimingPoint != null && NextTimingPoint.MusicPosition <= value) return;

            if (IsNewMusicPositionValid)
            {
                musicPosition = value;
                IsNewMusicPositionValid = false;
                // Update MPS for this timing point and the previous ones
                MeasuresPerSecond_Update();
            }

            NewMusicPosition = value;

            MusicPositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int CompareTo(TimingPoint? other) => Time.CompareTo(other?.Time);

    public object Clone()
    {
        var timingPoint = new TimingPoint
        {
            MusicPosition = MusicPosition,
            Time = Time,
            TimeSignature = TimeSignature,
            MeasuresPerSecond = MeasuresPerSecond,
            Bpm = Bpm,
            //PreviousTimingPoint = PreviousTimingPoint,
            //NextTimingPoint = NextTimingPoint
        };

        return timingPoint;
    }

    public event EventHandler UpdateMeasuresPerSecond = null!;
    /// <summary>
    /// Update <see cref="MeasuresPerSecond"/> based on this and next timing point's <see cref="Time"/> and <see cref="MusicPosition"/> values.
    /// </summary>
    public void MeasuresPerSecond_Update()
    {
        if (MusicPosition == null)
            return;
        UpdateMeasuresPerSecond?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Relies on parent <see cref="Timing" /> to delete from project.
    /// </summary>
    public void Delete() =>
        //if (PreviousTimingPoint != null) previousTimingPoint.NextTimingPoint = nextTimingPoint;
        //if (NextTimingPoint != null) nextTimingPoint.PreviousTimingPoint = previousTimingPoint;
        EmitSignal(nameof(Deleted), this);
}