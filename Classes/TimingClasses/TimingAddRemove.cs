using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

public partial class Timing
{
   
    /// <summary>
    /// Add a timing point. Primary constructor for loading timing points with a file.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <param name="time"></param>
    public void AddTimingPoint(float musicPosition, float time)
    {
        var timingPoint = new TimingPoint(time, musicPosition, GetTimeSignature(musicPosition));
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        timingPoint.IsInstantiating = false;

        int index = TimingPoints.IndexOf(timingPoint);

        if (index >= 1) // Set previous timing point
        {
            TimingPoints[index - 1].MeasuresPerSecond_Set(this);

            if (!IsInstantiating)
                Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
        }

        ActionsHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    /// Add a timing point and force a given <see cref="TimingPoint.MeasuresPerSecond"/> value without checking validity.
    /// </summary>
    public void AddTimingPoint(float musicPosition, float time, float measuresPerSecond)
    {
        var timingPoint = new TimingPoint(time, musicPosition, GetTimeSignature(musicPosition), measuresPerSecond);
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        timingPoint.IsInstantiating = false;

        if (!IsInstantiating)
            Signals.Instance.EmitEvent(Signals.Events.TimingChanged);

        ActionsHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    ///     Add a <see cref="TimingPoint"/> at a given time. 
    ///     The <see cref="TimingPoint.MusicPosition"/> is defined via the existing timing.
    /// </summary>
    /// <param name="time"></param>
    public void AddTimingPoint(float time, out TimingPoint? timingPoint)
    {
        timingPoint = new TimingPoint(time, GetTimeSignature(TimeToMusicPosition(time)));
        TimingPoints.Add(timingPoint);
        TimingPoints.Sort();

        SubscribeToEvents(timingPoint);

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        timingPoint.MusicPosition_Set(TimeToMusicPosition(time), this);

        if (timingPoint.MusicPosition == null
            || previousTimingPoint?.MusicPosition == timingPoint.MusicPosition
            || nextTimingPoint?.MusicPosition == timingPoint.MusicPosition
            || (previousTimingPoint?.MusicPosition is float previousMusicPosition && Mathf.Abs(previousMusicPosition - (float)timingPoint.MusicPosition) < 0.015f) // Too close to previous timing point
            || (nextTimingPoint?.MusicPosition is float nextMusicPosition && Mathf.Abs(nextMusicPosition - (float)timingPoint.MusicPosition) < 0.015f) // Too close to next timing point
           )
        {
            TimingPoints.Remove(timingPoint);
            timingPoint = null;
            //GD.Print("Timing Point refused to add!");
            return;
        }

        timingPoint.IsInstantiating = false;

        ActionsHandler.Instance.AddTimingMemento();

        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }

    private void DeleteTimingPoint(TimingPoint timingPoint)
    {
        TimingPoints.IndexOf(timingPoint);

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);

        timingPoint.QueueFree();
        TimingPoints.Remove(timingPoint);

        previousTimingPoint?.MeasuresPerSecond_Set(this);

        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);

        ActionsHandler.Instance.AddTimingMemento();
    }
     
}