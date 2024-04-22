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

    private float? heldTimingPoint_PreviousMusicPosition = null!;
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
                bool doesTimingPointStillExist = heldTimingPoint != null;
                bool hasMusicPositionChanged = heldTimingPoint?.MusicPosition != heldTimingPoint_PreviousMusicPosition;
                bool hasOffsetChanged = heldTimingPoint?.Offset != heldTimingPoint_PreviousOffset;
                if ((hasMusicPositionChanged || hasOffsetChanged) && doesTimingPointStillExist)
                {
                    MementoHandler.Instance.AddTimingMemento();
                }
                heldTimingPoint_PreviousMusicPosition = null;
                heldTimingPoint_PreviousOffset = null;
                heldTimingPoint = value;
                GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.Instance.TimingChanged), this, EventArgs.Empty); // Updates visuals to ensure no waveform sections are darkened.
                return;
            }
            heldTimingPoint = value;
            heldTimingPoint_PreviousMusicPosition = value?.MusicPosition;
            heldTimingPoint_PreviousOffset = value?.Offset;
        }
    }

    TimingPoint? litTimingPoint;
    public TimingPoint? LitTimingPoint
    {
        get => litTimingPoint;
        set
        {
            if (litTimingPoint == value)
                return;
            litTimingPoint = value;
            GlobalEvents.Instance.InvokeEvent(nameof(GlobalEvents.TimingPointLightUp), this, new GlobalEvents.ObjectArgument<TimingPoint?>(litTimingPoint));
        }
    }

    public bool IsSelectedMusicPositionMoving = false;

    private float selectedPosition;

    public float SelectedMusicPosition
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

    public static Context Instance { get => instance; set => instance = value; }

    public override void _Ready()
    {
        Instance = this;

        GlobalEvents.Instance.TimingPointHolding += OnTimingPointHolding;
        GlobalEvents.Instance.MouseLeftReleased += OnMouseLeftReleased;
    }

    private void OnTimingPointHolding(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<TimingPoint> timingPointArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(GlobalEvents.ObjectArgument<TimingPoint>)}");
        HeldTimingPoint = timingPointArgument.Value;
    }

    private void OnMouseLeftReleased(object? sender, EventArgs e) => HeldTimingPoint = null;
}