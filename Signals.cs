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
    [Signal] public delegate void MouseLeftReleasedEventHandler();
    [Signal] public delegate void ScrolledEventHandler();
    [Signal] public delegate void SettingsChangedEventHandler();
    [Signal] public delegate void SelectedPositionChangedEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }
}
