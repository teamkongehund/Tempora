using System;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

/// <summary>
/// A data class which asserts that a specific point in time (<see cref="Offset"/>)
/// should be attached to a musical timeline at (<see cref="MusicPosition"/>).
/// The Bpm (<see cref="Bpm"/>) is calculated via the subsequent <see cref="TimingPoint"/> 
/// in <see cref="Timing.TimingPoints"/> if the subsequent point exists.
/// </summary>
public partial class TimingPoint : Node, IComparable<TimingPoint>, ICloneable
{
    #region Properties and Fields

    public bool IsInstantiating = true;

    public ulong SystemTimeWhenCreatedMsec;

    #region Time Signature
    private int[] timeSignature = [4, 4];

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
                    EmitChangedEvent();
                return;
            }
            timeSignature = value;

            AreThereUncommunicatedChanges = true;
            RequestUpdateMPS();
        }
    }
    #endregion

    #region Time
    private float offset;

    /// <summary>
    /// The timestamp in the audio which this <see cref="TimingPoint"/> is attached to. 
    /// </summary>
    public float Offset
    {
        get => offset;
        private set
        {
            if (offset == value)
            {
                if (AreThereUncommunicatedChanges)
                    EmitChangedEvent();
                return;
            }

            offset = value;
            AreThereUncommunicatedChanges = true;
            RequestUpdateMPS();
            return;
        }
    }

    public void Offset_Set(float value, Timing timing)
    {
        TimingPoint? previousTimingPoint = timing.GetPreviousTimingPoint(this);
        TimingPoint? nextTimingPoint = timing.GetNextTimingPoint(this);

        // validity checks
        if (previousTimingPoint != null && previousTimingPoint.Offset >= value)
            return;
        if (nextTimingPoint != null && nextTimingPoint.Offset <= value)
            return;

        Offset = value;
    }

    #endregion

    #region MusicPosition
    private float? musicPosition;
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
        private set
        {
            if (musicPosition == value)
            {
                if (AreThereUncommunicatedChanges)
                    EmitChangedEvent();
                return;
            }

            musicPosition = value;
            AreThereUncommunicatedChanges = true;

            RequestUpdateMPS();
        }
    }

    public void MusicPosition_Set(float value, Timing timing)
    {
        bool isValid = timing.CanTimingPointGoHere(this, value, out TimingPoint? rejectingTimingPoint);

        if (rejectingTimingPoint != null)
            Signals.Instance.EmitEvent(Signals.Events.MusicPositionChangeRejected, new Signals.ObjectArgument<TimingPoint>(rejectingTimingPoint));

        if (isValid)
            MusicPosition = value;
    }
    #endregion

    #region MeasuresPerSecond
    private float measuresPerSecond = 0.5f;
    /// <summary>
    /// Musical measures per second. 
    /// Directly correlated with <see cref="Bpm"/> and <see cref="TimeSignature"/>
    /// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
    /// Cannot be changed directly, as it is a calculated property via <see cref="MeasuresPerSecond_Set(Timing)"/>
    /// </summary>
    public float MeasuresPerSecond
    {
        get => measuresPerSecond;
        private set
        {
            if (measuresPerSecond == value)
            {
                if (AreThereUncommunicatedChanges)
                    EmitChangedEvent();
                return;
            }

            measuresPerSecond = value;
            AreThereUncommunicatedChanges = true;
            Bpm = MpsToBpm(value);
        }
    }
    /// <summary>
    /// Sends a request to have both this point and the previous point's <see cref="MeasuresPerSecond"/>updated. 
    /// Handled by <see cref="Timing"/>.
    /// </summary>
    public void RequestUpdateMPS()
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
    public void MeasuresPerSecond_Set(Timing timing)
    {
        TimingPoint? previousTimingPoint = timing.GetPreviousTimingPoint(this);
        TimingPoint? nextTimingPoint = timing.GetNextTimingPoint(this);

        if (MusicPosition == null)
            throw new NullReferenceException(nameof(MusicPosition));

        if (nextTimingPoint?.MusicPosition != null)
        {
            MeasuresPerSecond =
                ((float)nextTimingPoint.MusicPosition - (float)MusicPosition)
                / (nextTimingPoint.Offset - Offset);
        }
        else if (previousTimingPoint?.MusicPosition != null)
        {
            MeasuresPerSecond =
                ((float)MusicPosition - (float)previousTimingPoint.MusicPosition)
                / (Offset - previousTimingPoint.Offset);
        }
        else if (AreThereUncommunicatedChanges)
        {
            EmitChangedEvent();
            return; // Make no changes
        }
    }
    #endregion

    #region Bpm
    private float bpm;
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
                bpm = MpsToBpm(MeasuresPerSecond);
            return bpm;
        }
        private set
        {
            if (bpm == value)
            {
                if (AreThereUncommunicatedChanges)
                    EmitChangedEvent();
                return;
            }

            bpm = value;
            AreThereUncommunicatedChanges = true;
            MeasuresPerSecond = BpmToMps(bpm);
            return;
        }
    }

    /// <summary>
    /// Sets the <see cref="Bpm"/> if the value is valid according to supplied timing
    /// </summary>
    public void Bpm_Set(float bpm, Timing timing)
    {
        TimingPoint? nextTimingPoint = timing.GetNextTimingPoint(this);

        // validity check
        if (nextTimingPoint != null)
            return;

        Bpm = bpm;
    }
    #endregion

    #endregion
    #region Constructors
    public TimingPoint(float time, int[] timeSignature)
    {
        this.offset = time;
        this.timeSignature = timeSignature;
        SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
    }

    public TimingPoint(float time, float musicPosition, float measuresPerSecond)
    {
        this.offset = time;
        this.musicPosition = musicPosition;
        this.measuresPerSecond = measuresPerSecond;
        SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
    }

    public TimingPoint(float time, float musicPosition, int[] timeSignature)
    {
        this.offset = time;
        this.musicPosition = musicPosition;
        this.timeSignature = timeSignature;
        SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
    }

    public TimingPoint(float time, float musicPosition, int[] timeSignature, float measuresPerSecond)
    {
        this.offset = time;
        this.musicPosition = musicPosition;
        this.timeSignature = timeSignature;
        this.measuresPerSecond = measuresPerSecond;
        bpm = MpsToBpm(measuresPerSecond);
        SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
    }

    /// <summary>
    /// Constructor used only for cloning
    /// </summary>
    private TimingPoint(float time, float? musicPosition, int[] timeSignature, float measuresPerSecond, float bpm, bool isInstantiating)
    {
        this.offset = time;
        this.musicPosition = musicPosition;
        this.timeSignature = timeSignature;
        this.measuresPerSecond = measuresPerSecond;
        this.bpm = bpm;
        this.IsInstantiating = isInstantiating;
        SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
    }
    #endregion
    #region Interface Methods
    public int CompareTo(TimingPoint? other) => Offset.CompareTo(other?.Offset);

    public object Clone()
    {
        var timingPoint = new TimingPoint(Offset, MusicPosition, TimeSignature, MeasuresPerSecond, Bpm, IsInstantiating);

        return timingPoint;
    }
    #endregion
    #region Change and Deletion Events
    public event EventHandler MPSUpdateRequested = null!;

    private bool AreThereUncommunicatedChanges = false;

    //public event EventHandler MusicPositionChangeRejected = null!;

    public event EventHandler Changed = null!;
    public void EmitChangedEvent()
    {
        if (!IsInstantiating)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            AreThereUncommunicatedChanges = false;
        }
    }

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