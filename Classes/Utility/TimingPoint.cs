using System;
using Godot;

namespace OsuTimer.Classes.Utility;

/// <summary>
/// A data class which asserts that a specific point in time (<see cref="Time"/>)
/// should be attached to a musical timeline in the position (<see cref="MusicPosition"/>).
/// The Bpm (<see cref="Bpm"/>) is calculated via the subsequent <see cref="TimingPoint"/> 
/// in <see cref="Timing.TimingPoints"/> if the subsequent point exists.
/// </summary>
public partial class TimingPoint : Node, IComparable<TimingPoint>, ICloneable
{
    #region Properties and Fields

    public bool IsInstantiating = true;

    #region Time Signature
    private int[] timeSignature = [4, 4];
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

    /// <summary>
    /// Time Signature. Affects how <see cref="MeasuresPerSecond"/> and <see cref="TimeSignature"/> correlate
    /// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
    /// Changes are always accepted.
    /// </summary>
    public int[] TimeSignature
    {
        get => timeSignature;
        set
        {
            if (timeSignature == value)
            {
                if (AreThereUncommunicatedChanges)
                {
                    EmitChangedEvent();
                    AreThereUncommunicatedChanges = false;
                }
                return;
            }
            timeSignature = value;

            AreThereUncommunicatedChanges = true;
            RequestTimingToUpdateMPS();
        }
    }
    #endregion

    #region Time
    private float time;
    public float NewTime;
    public bool IsNewTimeValid = false;

    /// <summary>
    /// The timestamp in the audio which this <see cref="TimingPoint"/> is attached to. 
    /// </summary>
    public float Time
    {
        get => time;
        set
        {
            if (time == value)
            {
                if (AreThereUncommunicatedChanges)
                {
                    EmitChangedEvent();
                    AreThereUncommunicatedChanges = false;
                }
                return;
            }
            //if (PreviousTimingPoint != null && PreviousTimingPoint.Time >= value) return;
            //if (NextTimingPoint != null && NextTimingPoint.Time <= value) return;

            if (IsNewTimeValid)
            {
                time = value;
                IsNewTimeValid = false;
                AreThereUncommunicatedChanges = true;
                RequestTimingToUpdateMPS();
                return;
            }

            NewTime = value;

            AttemptChangeTime?.Invoke(this, EventArgs.Empty); // Received by Timing class
        }
    }
    #endregion

    #region MusicPosition
    private float? musicPosition;
    public float? NewMusicPosition;
    public bool IsNewMusicPositionValid = false;
    /// <summary>
    /// The <see cref="TimingPoint"/>'s position on the musical timeline. 
    /// The value is defined in terms of measures (integer part) from the musical timeline origin.
    /// Individual beats in a measure are the fractional part of the value.
    /// As an example, if a measure has a 4/4 <see cref="TimeSignature"/>, 
    /// the value 0.75 means "Measure 0, Quarter note 4"
    /// </summary>
    public float? MusicPosition
    {
        get => musicPosition;
        set
        {
            if (musicPosition == value)
            {
                if (AreThereUncommunicatedChanges)
                {
                    EmitChangedEvent();
                    AreThereUncommunicatedChanges = false;
                }
                return;
            }
            //if (PreviousTimingPoint != null && PreviousTimingPoint.MusicPosition >= value) return;
            //if (NextTimingPoint != null && NextTimingPoint.MusicPosition <= value) return;

            if (IsNewMusicPositionValid)
            {
                musicPosition = value;
                IsNewMusicPositionValid = false;
                AreThereUncommunicatedChanges = true;
                // Update MPS for this timing point and the previous ones
                RequestTimingToUpdateMPS();
            }

            NewMusicPosition = value;

            AttemptChangeMusicPosition?.Invoke(this, EventArgs.Empty);
        }
    }
    #endregion

    #region MeasuresPerSecond
    private float measuresPerSecond = 0.5f;
    /// <summary>
    /// Musical measures per second. 
    /// Directly correlated with <see cref="Bpm"/> and <see cref="TimeSignature"/>
    /// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
    /// Cannot be changed directly, as it is a calculated property via <see cref="UpdateMeasuresPerSecond(Timing)"/>
    /// </summary>
    public float MeasuresPerSecond
    {
        get => measuresPerSecond;
        private set
        {
            if (measuresPerSecond == value)
            {
                if (AreThereUncommunicatedChanges)
                {
                    EmitChangedEvent();
                    AreThereUncommunicatedChanges = false;
                }
                return;
            }
            {
                measuresPerSecond = value;
                AreThereUncommunicatedChanges = true;
                Bpm = MpsToBpm(value);
            }
        }
    }
    /// <summary>
    /// Sends a request to have its <see cref="MeasuresPerSecond"/>updated. Handled by <see cref="Timing"/>.
    /// </summary>
    public void RequestTimingToUpdateMPS()
    {
        if (MusicPosition == null)
            return;
        MPSUpdateRequested?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>
    /// Combined with the private <see cref="MeasuresPerSecond"/> setter, 
    /// ensures that the value can only by set according to <see cref="Timing"/>
    /// </summary>
    /// <param name="timing"></param>
    public void UpdateMeasuresPerSecond(Timing timing)
    {
        TimingPoint? previousTimingPoint = timing.GetPreviousTimingPoint(this);
        TimingPoint? nextTimingPoint = timing.GetNextTimingPoint(this);

        if (MusicPosition == null)
            throw new NullReferenceException(nameof(MusicPosition));

        if (previousTimingPoint?.MusicPosition != null)
        {
            previousTimingPoint.MeasuresPerSecond =
                ((float)MusicPosition - (float)previousTimingPoint.MusicPosition)
                / (Time - previousTimingPoint.Time);
            MeasuresPerSecond = previousTimingPoint.MeasuresPerSecond;
        }

        if (nextTimingPoint?.MusicPosition != null)
        {
            MeasuresPerSecond =
                ((float)nextTimingPoint.MusicPosition - (float)MusicPosition)
                / (nextTimingPoint.Time - Time);
        }
    }
    #endregion

    #region Bpm
    private float bpm;
    public float NewBpm;
    public bool IsNewBpmValid = false;
    /// <summary>
    /// Beats per minute. Directly correlated with <see cref="MeasuresPerSecond"/> and <see cref="TimeSignature"/>
    /// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
    /// Changes are only accepted if <see cref="Timing"/> validates the change.
    /// </summary>
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
            {
                if (AreThereUncommunicatedChanges)
                {
                    EmitChangedEvent();
                    AreThereUncommunicatedChanges = false;
                }
                return;
            }
            else if (IsNewBpmValid)
            {
                bpm = value;
                IsNewBpmValid = false;
                AreThereUncommunicatedChanges = true;
                MeasuresPerSecond = BpmToMps(bpm);
                return;
            }

            NewBpm = value;

            AttemptChangeBpm?.Invoke(this, EventArgs.Empty); // Received by Timing class
        }
    } 
    #endregion

    #endregion
    #region Constructors
    public TimingPoint(float time, int[] timeSignature)
    {
        this.time = time;
        this.timeSignature = timeSignature;
    }

    public TimingPoint(float time, float musicPosition, float measuresPerSecond)
    {
        this.time = time;
        this.musicPosition = musicPosition;
        this.measuresPerSecond = measuresPerSecond;
    }

    public TimingPoint(float time, float musicPosition, int[] timeSignature)
    {
        this.time = time;
        this.musicPosition = musicPosition;
        this.timeSignature = timeSignature;
    }

    public TimingPoint(float time, float musicPosition, int[] timeSignature, float measuresPerSecond)
    {
        this.time = time;
        this.musicPosition = musicPosition;
        this.timeSignature = timeSignature;
        this.measuresPerSecond = measuresPerSecond;
        bpm = MpsToBpm(measuresPerSecond);
    }

    /// <summary>
    /// Constructor used only for cloning
    /// </summary>
    /// <param name="time"></param>
    /// <param name="musicPosition"></param>
    /// <param name="timeSignature"></param>
    /// <param name="measuresPerSecond"></param>
    /// <param name="bpm"></param>
    private TimingPoint(float time, float? musicPosition, int[] timeSignature, float measuresPerSecond, float bpm)
    {
        this.time = time;
        this.musicPosition = musicPosition;
        this.timeSignature = timeSignature;
        this.measuresPerSecond = measuresPerSecond;
        this.bpm = bpm;
    }
    #endregion
    #region Interface Methods
    public int CompareTo(TimingPoint? other) => Time.CompareTo(other?.Time);

    public object Clone()
    {
        var timingPoint = new TimingPoint(Time, MusicPosition, TimeSignature, MeasuresPerSecond, Bpm);

        return timingPoint;
    }
    #endregion
    #region Change and Deletion Events
    public event EventHandler AttemptChangeTime = null!;
    public event EventHandler AttemptChangeMusicPosition = null!;
    public event EventHandler MPSUpdateRequested = null!;
    public event EventHandler AttemptChangeBpm = null!;

    private bool AreThereUncommunicatedChanges = false;
    public event EventHandler Changed = null!;
    public void EmitChangedEvent() => Changed?.Invoke(this, EventArgs.Empty);

    public event EventHandler AttemptDelete = null!;
    /// <summary>
    ///     Relies on parent <see cref="Timing" /> to delete from project.
    /// </summary>
    public void Delete() => AttemptDelete?.Invoke(this, EventArgs.Empty);
    #endregion
    #region Calculators
    private float BpmToMps(float bpm) => bpm / (60 * (TimeSignature[0] * 4f / TimeSignature[1]));

    private float MpsToBpm(float mps) => mps * 60 * (TimeSignature[0] * 4f / TimeSignature[1]);

    public float BeatLengthSec => 1 / (Bpm / 60); 
    #endregion
}