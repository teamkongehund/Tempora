using Godot;
using System;

/// <summary>
/// Singleton class that sends signals to keep everything updated when properties change.
/// </summary>
public partial class Signals : Node
{
	public static Signals Instance;

    [Signal] public delegate void TimingChangedEventHandler();

    [Signal] public delegate void TimingPointHoldingEventHandler(TimingPoint timingPoint);
    public TimingPoint HeldTimingPoint;

    [Signal] public delegate void MouseLeftReleasedEventHandler();

    [Signal] public delegate void ScrolledEventHandler();

    public override void _Ready()
    {
        Instance = this;

        TimingPointHolding += OnTimingPointHolding;
        MouseLeftReleased += OnMouseLeftReleased;
    }

    public void OnTimingPointHolding(TimingPoint timingPoint)
    {
        HeldTimingPoint = timingPoint;
        GD.Print($"Signals - HeldTimingPoint MusicPosition = {timingPoint?.MusicPosition}");
    }

    public void OnMouseLeftReleased()
    {
        HeldTimingPoint = null;
    }
}
