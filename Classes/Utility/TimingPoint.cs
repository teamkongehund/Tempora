using System;
using Godot;

namespace OsuTimer.Classes.Utility;

public partial class TimingPoint : Node, IComparable<TimingPoint> {
    [Signal]
    public delegate void ChangedEventHandler(TimingPoint timingPoint);

    [Signal]
    public delegate void DeletedEventHandler(TimingPoint timingPoint);

    private float bpm;

    /// <summary>
    ///     The tempo from this timing point until the next. The value is proportional to BPM if the time signature doesn't
    ///     change.
    /// </summary>
    private float measuresPerSecond = 0.5f;

    private float? musicPosition;
    private TimingPoint nextTimingPoint;

    private TimingPoint previousTimingPoint;

    private float time;

    private int[] timeSignature = { 4, 4 };

    public TimingPoint PreviousTimingPoint {
        get => previousTimingPoint;
        set {
            if (previousTimingPoint == value) return;
            previousTimingPoint = value;
            if (PreviousTimingPoint != null) PreviousTimingPoint.NextTimingPoint = this;
            MeasuresPerSecond_Update();
        }
    }

    public TimingPoint NextTimingPoint {
        get => nextTimingPoint;
        set {
            if (nextTimingPoint == value) return;
            nextTimingPoint = value;
            if (NextTimingPoint != null) NextTimingPoint.PreviousTimingPoint = this;
            MeasuresPerSecond_Update();
        }
    }

    public int[] TimeSignature {
        get => timeSignature;
        set {
            timeSignature = value;
            MeasuresPerSecond_Update();
        }
    }

    public float Time {
        get => time;
        set {
            if (time == value) return;
            if (PreviousTimingPoint != null && PreviousTimingPoint.Time >= value) return;
            if (NextTimingPoint != null && NextTimingPoint.Time <= value) return;

            time = value;
            MeasuresPerSecond_Update();

            EmitSignal(nameof(Changed), this);
        }
    }

    public float MeasuresPerSecond {
        get => measuresPerSecond;
        set {
            if (measuresPerSecond != value) {
                measuresPerSecond = value;
                BPM_Update();
            }
        }
    }

    public float Bpm {
        get {
            if (bpm == 0) BPM_Update();
            return bpm;
        }
        private set {
            if (bpm == value) return;
            bpm = value;
        }
    }
    public void BPM_Update(float bpm) {
        if (NextTimingPoint != null)
            return;
        Bpm = bpm;
        MeasuresPerSecond = Bpm / (60 * (TimeSignature[0] * 4f / TimeSignature[1]));
        EmitSignal(nameof(Changed), this);
    }

    public float BeatLength => 1 / (Bpm / 60);

    public float? MusicPosition {
        get => musicPosition;
        set {
            if (musicPosition == value) return;
            if (PreviousTimingPoint != null && PreviousTimingPoint.MusicPosition >= value) return;
            if (NextTimingPoint != null && NextTimingPoint.MusicPosition <= value) return;

            musicPosition = value;

            // Update MPS for this timing point and the previous one
            MeasuresPerSecond_Update();

            EmitSignal(nameof(Changed), this);
        }
    }

    public int CompareTo(TimingPoint other) {
        return Time.CompareTo(other.Time);
    }

    public void MeasuresPerSecond_Update() {
        if (MusicPosition == null)
            return;

        if (PreviousTimingPoint?.MusicPosition != null) {
            PreviousTimingPoint.MeasuresPerSecond =
                ((float)MusicPosition - (float)PreviousTimingPoint.MusicPosition)
                / (Time - PreviousTimingPoint.Time);
            MeasuresPerSecond = PreviousTimingPoint.MeasuresPerSecond;
        }

        if (NextTimingPoint?.MusicPosition != null)
            MeasuresPerSecond =
                ((float)NextTimingPoint.MusicPosition - (float)MusicPosition)
                / (NextTimingPoint.Time - Time);
    }

    public void BPM_Update() {
        Bpm = MeasuresPerSecond * 60 * (TimeSignature[0] * 4f / TimeSignature[1]);
    }


    /// <summary>
    ///     Relies on parent <see cref="Timing" /> to delete from project.
    /// </summary>
    public void Delete() {
        if (PreviousTimingPoint != null) PreviousTimingPoint.NextTimingPoint = NextTimingPoint;
        if (NextTimingPoint != null) NextTimingPoint.PreviousTimingPoint = PreviousTimingPoint;
        EmitSignal(nameof(Deleted), this);
    }
}