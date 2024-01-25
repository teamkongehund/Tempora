using System;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    public event EventHandler TimeSignaturesChanged = null!;

    private void SubscribeToEvents(TimingPoint timingPoint)
    {
        if (timingPoint == null)
            throw new NullReferenceException($"{nameof(timingPoint)} was null");
        timingPoint.AttemptDelete += OnTimingPointDeleteAttempt;
        timingPoint.MPSUpdateRequested += OnTimingPointRequestingToUpdateMPS;
        timingPoint.Changed += OnTimingPointChanged;
    }

    private void ReSubscribe()
    {
        foreach (TimingPoint timingPoint in timingPoints)
        {
            SubscribeToEvents(timingPoint);
        }
    }

    private void OnTimingPointChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");

        if (!timingPoint.IsInstantiating)
            Signals.Instance.EmitEvent(Signals.Events.TimingChanged);
    }
    private void OnTimingPointDeleteAttempt(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        DeleteTimingPoint(timingPoint);
    }
    private void OnTimingPointRequestingToUpdateMPS(object? sender, EventArgs e)
    {
        if (IsBatchOperatingInProgress)
            return; // defer update till after the operation is complete.
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"Request to update {nameof(timingPoint.MeasuresPerSecond)} failed because {nameof(timingPoint.MusicPosition)} is null");

        timingPoint.MeasuresPerSecond_Set(this);
        if (!timingPoint.IsInstantiating)
            GetPreviousTimingPoint(timingPoint)?.MeasuresPerSecond_Set(this);
    }
}
