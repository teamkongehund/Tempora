using System;
using Godot;

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
                    ActionsHandler.Instance.AddTimingMemento();
                }
                heldTimingPoint_PreviousMusicPosition = null;
                heldTimingPoint_PreviousOffset = null;
                heldTimingPoint = value;
                Signals.Instance.EmitEvent(Signals.Events.TimingChanged); // Updates visuals to ensure no waveform sections are darkened.
                return;
            }
            heldTimingPoint = value;
            heldTimingPoint_PreviousMusicPosition = value?.MusicPosition;
            heldTimingPoint_PreviousOffset = value?.Offset;
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
            Signals.Instance.EmitEvent(Signals.Events.SelectedPositionChanged);
        }
    }

    public static Context Instance { get => instance; set => instance = value; }

    public override void _Ready()
    {
        Instance = this;

        Signals.Instance.TimingPointHolding += OnTimingPointHolding;
        Signals.Instance.MouseLeftReleased += OnMouseLeftReleased;
    }

    private void OnTimingPointHolding(object? sender, EventArgs e)
    {
        if (e is not Signals.ObjectArgument<TimingPoint> timingPointArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(Signals.ObjectArgument<TimingPoint>)}");
        HeldTimingPoint = timingPointArgument.Value;
    }

    private void OnMouseLeftReleased(object? sender, EventArgs e) => HeldTimingPoint = null;
}