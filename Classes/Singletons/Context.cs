// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using Godot;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Utility;

/// <summary>
///     Manages user context, such as object selection
/// </summary>
public partial class Context : Node
{
    private static Context instance = null!;

    public bool ShouldDeleteHeldPointIfNotOnGrid = false;
    public bool HeldPointIsJustBeingAdded = false;
    private float? heldTimingPoint_PreviousMeasurePosition = null!;
    private float? heldTimingPoint_PreviousOffset = null!;
    private TimingPoint? heldTimingPoint = null!;
    public TimingPoint? HeldTimingPoint
    {
        get => heldTimingPoint;
        set
        {
            if (heldTimingPoint == value)
                return;
            if (value == null)
            {
                if (HeldPointIsJustBeingAdded && !Timing.Instance.IsTimingPointOnGrid(heldTimingPoint))
                {
                    TimingPoint? timingPointToDelete = heldTimingPoint;
                    heldTimingPoint_PreviousMeasurePosition = null;
                    heldTimingPoint_PreviousOffset = null;
                    heldTimingPoint = null;
                    timingPointToDelete?.Delete();
                    GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingChanged)); // Gets rid of VisualTimingPoint
                    HeldPointIsJustBeingAdded = false;
                    return;
                }
                bool doesTimingPointStillExist = heldTimingPoint != null;
                bool hasMeasurePositionChanged = heldTimingPoint?.MeasurePosition != heldTimingPoint_PreviousMeasurePosition;
                bool hasOffsetChanged = heldTimingPoint?.Offset != heldTimingPoint_PreviousOffset;
                //if ((hasMeasurePositionChanged || hasOffsetChanged) && doesTimingPointStillExist && !HeldPointIsJustBeingAdded)
                //{
                //    MementoHandler.Instance.AddTimingMemento();
                //}
                if (HeldPointIsJustBeingAdded && doesTimingPointStillExist)
                {
                    GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Instance.TimingPointAdded), this, new GlobalEvents.ObjectArgument<TimingPoint>(heldTimingPoint!));
                    MementoHandler.Instance.AddTimingMemento();
                }
                HeldPointIsJustBeingAdded = false;
                heldTimingPoint_PreviousMeasurePosition = null;
                heldTimingPoint_PreviousOffset = null;
                heldTimingPoint = value;
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Instance.TimingChanged), this, EventArgs.Empty); // Updates visuals to ensure no waveform sections are darkened.
                return;
            }
            heldTimingPoint = value;
            heldTimingPoint_PreviousMeasurePosition = value?.MeasurePosition;
            heldTimingPoint_PreviousOffset = value?.Offset;
        }
    }

    TimingPoint? timingPointNearestCursor;
    public TimingPoint? TimingPointNearestCursor
    {
        get => timingPointNearestCursor;
        set
        {
            if (timingPointNearestCursor == value)
                return;
            timingPointNearestCursor = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointNearestCursorChanged), this, new GlobalEvents.ObjectArgument<TimingPoint?>(timingPointNearestCursor));
        }
    }

    public bool IsSelectedMeasurePositionMoving = false;

    private float selectedPosition;

    public float SelectedMeasurePosition
    {
        get => selectedPosition;
        set
        {
            if (value == selectedPosition)
                return;
            selectedPosition = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Instance.SelectedPositionChanged), this, EventArgs.Empty);
        }
    }

    public Control? FocusedControl = null!;

    public static Context Instance { get => instance; set => instance = value; }

    public override void _Ready()
    {
        Instance = this;

        GlobalEvents.Instance.TimingPointHolding += OnTimingPointHolding;
        GlobalEvents.Instance.MouseLeftReleased += OnMouseLeftReleased;
        GlobalEvents.Instance.MeasurePositionChangeRejected += OnTimingPointMeasurePositionRejected;

        GetViewport().GuiFocusChanged += OnGuiFocusChanged;
    }

    private void OnTimingPointHolding(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<TimingPoint>)}");
        HeldTimingPoint = timingPointArgument.Value;
    }

    private void OnMouseLeftReleased(object? sender, EventArgs e) => HeldTimingPoint = null;

    private void OnGuiFocusChanged(Control focusedControl)
    {
        FocusedControl = focusedControl;
    }

    private void OnTimingPointMeasurePositionRejected(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            return;
    }

    public bool AreAnySubwindowsVisible => GetViewport().GetEmbeddedSubwindows().Count > 0;
}