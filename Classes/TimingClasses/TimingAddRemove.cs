using System;
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
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
        }
        //if (index < TimingPoints.Count - 1)
        //{
        //    timingPoint.MeasuresPerSecond_Set(this);

        //    if (!IsInstantiating)
        //        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
        //}

        //ActionsHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    /// Add a timing point and deduce time and MPS from other Timing Points. Useful for adding extra points on downbeats and quarter notes.
    /// This method should not be accessible from the GUI.
    /// </summary>
    /// <param name="musicPosition"></param>
    /// <param name="time"></param>
    public void AddTimingPoint(float musicPosition)
    {
        var timingPoint = new TimingPoint(musicPosition);
        timingPoint.Offset_Set(MusicPositionToSampleTime(musicPosition), this);
        TimingPoints.Add(timingPoint);
        SubscribeToEvents(timingPoint);
        TimingPoints.Sort();

        // This should also update MPS
        timingPoint.TimeSignature = GetTimeSignature(musicPosition);

        timingPoint.IsInstantiating = false;

        // No timing memento added, as this isn't method shouldn't be executed directly from a user action.
        //ActionsHandler.Instance.AddTimingMemento();
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
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));

        int index = TimingPoints.IndexOf(timingPoint);

        if (index >= 1) // Set previous timing point
        {
            TimingPoints[index - 1].MeasuresPerSecond_Set(this);

            if (!IsInstantiating)
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
        }
    }

    /// <summary>
    ///     Add a <see cref="TimingPoint"/> at a given time. Primary GUI method.
    ///     The <see cref="TimingPoint.MusicPosition"/> is defined via the existing timing.
    /// </summary>
    /// <param name="time"></param>
    public void AddTimingPoint(float time, out TimingPoint? timingPoint)
    {
        timingPoint = new TimingPoint(time, GetTimeSignature(SampleTimeToMusicPosition(time)));
        TimingPoints.Add(timingPoint);
        TimingPoints.Sort();

        SubscribeToEvents(timingPoint);

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        timingPoint.MusicPosition_Set(SampleTimeToMusicPosition(time), this);

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

        //ActionsHandler.Instance.AddTimingMemento();

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));
    }

    private void DeleteTimingPoint(TimingPoint timingPoint)
    {
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);

        timingPoint.QueueFree();
        TimingPoints.Remove(timingPoint);

        previousTimingPoint?.MeasuresPerSecond_Set(this);

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));

        MementoHandler.Instance.AddTimingMemento();
    }

    /// <summary>
    /// Delete selection if the <see cref="TimingPoint"/> is in the selection. Otherwise, delete the timing point.
    /// </summary>
    /// <param name="timingPoint"></param>
    public void DeleteTimingPointOrSelection(TimingPoint? timingPoint)
    {
        if (timingPoint == null)
            return;
        if (TimingPointSelection.Instance.IsPointInSelection(timingPoint))
            TimingPointSelection.Instance.DeleteSelection();
        else
            DeleteTimingPoint(timingPoint);
    }

    /// <summary>
    /// Deletes timing points between index excluding indexTo.
    /// </summary>
    /// <param name="indexFrom"></param>
    /// <param name="indexTo"></param>
    public void DeleteTimingPoints(int indexFrom, int indexTo)
    {
        if (TimingPoints.Count == 0) return;
        TimingPoint? previousTimingPoint = (indexFrom - 1) >= 0 ? TimingPoints[(indexFrom - 1)] : null;
        bool isIndexToLast = indexTo == TimingPoints.Count;

        for (int i = indexFrom; i < indexTo; i++)
        {
            TimingPoint timingPoint = TimingPoints[i];
            timingPoint.QueueFree();
        }
        TimingPoints.RemoveRange(indexFrom, indexTo - indexFrom);

        if (!isIndexToLast)
            previousTimingPoint?.MeasuresPerSecond_Set(this);

        GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointCountChanged));

        MementoHandler.Instance.AddTimingMemento();
    }

    public void DeleteAllTimingPoints()
    {
        DeleteTimingPoints(0, TimingPoints.Count);
    }
}