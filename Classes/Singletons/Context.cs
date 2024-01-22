using System;
using Godot;

namespace Tempora.Classes.Utility;

/// <summary>
///     Manages user context, such as object selection
/// </summary>
public partial class Context : Node
{
    private static Context instance = null!;

    public TimingPoint? HeldTimingPoint = null!;

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