using System;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Godot;
using NAudio.MediaFoundation;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;
public partial class Timing
{
    private void SubscribeToEvents(TimingPoint timingPoint)
    {
        if (timingPoint == null)
            throw new NullReferenceException($"{nameof(timingPoint)} was null");
        timingPoint.AttemptDelete += OnTimingPointDeleteAttempt;
        timingPoint.ChangeFinalized += OnTimingPointChanged;
        timingPoint.PropertyChanged += OnTimingPointPropertyChanged;
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

        if (!timingPoint.IsInstantiating && !IsBatchOperationInProgress)
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged));
    }
    private void OnTimingPointDeleteAttempt(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender wasn't a TimingPoint.");
        DeleteTimingPoint(timingPoint);
    }

    private void OnTimingPointMusicPositionChangeRejected(object? sender, EventArgs e)
    {
        if (IsBatchOperationInProgress)
            ShouldCancelBatchOperation = true;
    }

    /// <summary>
    /// Calculates the <see cref="TimingPoint.MeasuresPerSecond"/> of a <see cref="TimingPoint"/> based on adjacent points. Returns null if the value is free to be anything.
    /// </summary>
    /// <param name="timingPoint"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="Exception"></exception>
    private float? CalculateMPSBasedOnAdjacentPoints(TimingPoint timingPoint)
    {
        float? correctValue = null;
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);

        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException(nameof(timingPoint.MusicPosition));
        if (nextTimingPoint?.MusicPosition == timingPoint.MusicPosition || previousTimingPoint?.MusicPosition == timingPoint.MusicPosition)
            throw new Exception("Neighboring Timing Point has same Music Position.");

        if (nextTimingPoint?.MusicPosition != null)
        {
            correctValue =
                ((float)nextTimingPoint.MusicPosition - (float)timingPoint.MusicPosition)
                / (nextTimingPoint.Offset - timingPoint.Offset);
        }
        else if (previousTimingPoint?.MusicPosition != null)
        {
            float timeSignatureCorrection = ((float)previousTimingPoint.TimeSignature[0] / previousTimingPoint.TimeSignature[1]) / ((float)timingPoint.TimeSignature[0] / timingPoint.TimeSignature[1]);

            correctValue =
                ((float)timingPoint.MusicPosition - (float)previousTimingPoint.MusicPosition)
                / (timingPoint.Offset - previousTimingPoint.Offset)
                * timeSignatureCorrection;
        }

        return correctValue;
    }

    /// <summary>
    /// Updates adjacent <see cref="TimingPoint.MeasuresPerSecond"/>. These updates will invoke their own <see cref="TimingPoint.PropertyChanged"/>, which updates <see cref="TimingPoint.Bpm"/>.
    /// </summary>
    /// <param name="timingPoint"></param>
    private void UpdateAdjacentTempo(TimingPoint timingPoint)
    {
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);
        if (!timingPoint.IsInstantiating && previousTimingPoint != null)
            previousTimingPoint.MeasuresPerSecond = CalculateMPSBasedOnAdjacentPoints(previousTimingPoint) ?? previousTimingPoint.MeasuresPerSecond;
        if (!timingPoint.IsInstantiating && nextTimingPoint == TimingPoints[^1])
            nextTimingPoint.MeasuresPerSecond = CalculateMPSBasedOnAdjacentPoints(nextTimingPoint) ?? nextTimingPoint.MeasuresPerSecond;
    }

    private void UpdateAllDependentProperties(TimingPoint timingPoint, TimingPoint.PropertyType propertyType, object? value)
    {
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);
        switch (propertyType)
        {
            case TimingPoint.PropertyType.Offset:
                UpdateTempoIncludingAdjacent(timingPoint);
                break;
            case TimingPoint.PropertyType.MusicPosition:
                UpdateTempoIncludingAdjacent(timingPoint);
                break;
            case TimingPoint.PropertyType.MeasuresPerSecond:
                timingPoint.Bpm = timingPoint.MpsToBpm(timingPoint.MeasuresPerSecond);
                break;
            case TimingPoint.PropertyType.TimeSignature:
                UpdateTempoIncludingAdjacent(timingPoint);
                break;
            case TimingPoint.PropertyType.Bpm:
                timingPoint.MeasuresPerSecond = timingPoint.BpmToMps(timingPoint.Bpm);
                break;

        }
    }

    /// <summary>
    /// Ensures that all tempo-related value for this <see cref="TimingPoint"/> and adjacent ones are properly updated.
    /// </summary>
    /// <param name="timingPoint"></param>
    private void UpdateTempoIncludingAdjacent(TimingPoint timingPoint)
    {
        UpdateMPS(timingPoint);
        timingPoint.Bpm = timingPoint.MpsToBpm(timingPoint.MeasuresPerSecond);
        UpdateAdjacentTempo(timingPoint);
    }

    private void UpdateMPS(TimingPoint timingPoint) => timingPoint.MeasuresPerSecond = CalculateMPSBasedOnAdjacentPoints(timingPoint) ?? timingPoint.MeasuresPerSecond;

    /// <summary>
    /// When the <see cref="Timing"/> decides that changes to a timing point have been finalized, invoke an event that other classes can respond to.
    /// </summary>
    /// <param name="timingPoint"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private void FinalizeTimingPointChange(TimingPoint timingPoint)
    {
        if (timingPoint == null)
            throw new ArgumentNullException(nameof(timingPoint));
        timingPoint.FinalizeChange();
    }

    private void OnTimingPointPropertyChanged(object? sender, EventArgs e)
    {
        if (sender is null) 
            return;
        if (e is null)
            return;
        if (e is not TimingPoint.PropertyChangeArgument propertyChangeArgument)
            throw new Exception("The argument of a TimingPoint property change event must be of type TimingPoint.PropertyChangeArgument.");
        if (sender is not TimingPoint timingPoint)
            throw new Exception("Sender must be a TimingPoint");

        if (timingPoint.IsBeingUpdated)
            return; // All property updates should happen in one event call to prevent recursively invoking change events.

        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);
        var propertyType = propertyChangeArgument.PropertyType;
        var oldValue = propertyChangeArgument.OldValue;
        var newValue = propertyChangeArgument.NewValue;

        bool isValid = IsTimingPointPropertyChangeValid(timingPoint, propertyType, newValue);

        if (isValid)
        {
            timingPoint.IsBeingUpdated = true;
            UpdateAllDependentProperties(timingPoint, propertyType, newValue);
            timingPoint.IsBeingUpdated = false;
            FinalizeTimingPointChange(timingPoint);
            return;
        }

        switch (propertyType) // Reset to old values if change is denied
        {
            case TimingPoint.PropertyType.Offset:
                float oldOffset = (float)oldValue!;
                timingPoint.Offset = oldOffset;
                break;
            case TimingPoint.PropertyType.MeasuresPerSecond:
                float oldMPS = (float)oldValue!;
                timingPoint.MeasuresPerSecond = oldMPS;
                break;
            case TimingPoint.PropertyType.TimeSignature:
                int[] oldTimeSignature = (int[])oldValue!;
                timingPoint.TimeSignature = oldTimeSignature;
                break;
            case TimingPoint.PropertyType.Bpm:
                float oldBpm = (float)oldValue!;
                timingPoint.Bpm = oldBpm;
                break;
            case TimingPoint.PropertyType.MusicPosition:
                float? oldMusicPosition = (float?)oldValue;
                timingPoint.MusicPosition = oldMusicPosition;
                TimingPoint? rejectingTimingPoint = null;
                CanTimingPointGoHere(timingPoint, (float)newValue!, out rejectingTimingPoint);
                if (rejectingTimingPoint != null)
                    GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.MusicPositionChangeRejected), new GlobalEvents.ObjectArgument<TimingPoint>(rejectingTimingPoint));
                break;
            default:
                break;
        }
    }

    private bool IsTimingPointPropertyChangeValid(TimingPoint timingPoint, TimingPoint.PropertyType propertyType, object? newValue)
    {
        TimingPoint? previousTimingPoint = GetPreviousTimingPoint(timingPoint);
        TimingPoint? nextTimingPoint = GetNextTimingPoint(timingPoint);
        switch (propertyType)
        {
            case TimingPoint.PropertyType.Offset:
                var newOffset = (float)newValue!;
                bool previousPointBlocksChange = previousTimingPoint != null && previousTimingPoint.Offset >= newOffset;
                bool nextPointBlockschange = nextTimingPoint != null && nextTimingPoint.Offset <= newOffset;
                if (previousPointBlocksChange || nextPointBlockschange)
                    return false;
                return true;

            case TimingPoint.PropertyType.TimeSignature:
                return true;

            case TimingPoint.PropertyType.MeasuresPerSecond:
                if (nextTimingPoint == null)
                    return true;

                float? calculatedMPS = CalculateMPSBasedOnAdjacentPoints(timingPoint);

                return (float)newValue! == calculatedMPS!;

            case TimingPoint.PropertyType.Bpm:
                if (nextTimingPoint == null)
                    return true;

                calculatedMPS = CalculateMPSBasedOnAdjacentPoints(timingPoint);

                if (calculatedMPS == null)
                    return true;

                float newBpm = (float)newValue!;
                float calculatedBpm = timingPoint.MpsToBpm((float)calculatedMPS);

                if (Math.Abs(calculatedBpm - newBpm) < 0.0001)
                    return true;

                return false;

            case TimingPoint.PropertyType.MusicPosition:
                return CanTimingPointGoHere(timingPoint, (float)newValue!, out _);

            default:
                return false;
        }
    }
}
