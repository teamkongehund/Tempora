using Godot;

namespace OsuTimer.Classes.Utility;

/// <summary>
///     Singleton class that sends signals to keep everything updated when properties change.
/// </summary>
public partial class Signals : Node
{
    [Signal]
    public delegate void AudioFileChangedEventHandler();

    [Signal]
    public delegate void MouseLeftReleasedEventHandler();

    [Signal]
    public delegate void ScrolledEventHandler();

    [Signal]
    public delegate void SelectedPositionChangedEventHandler();

    [Signal]
    public delegate void SettingsChangedEventHandler();

    [Signal]
    public delegate void TimingChangedEventHandler();

    [Signal]
    public delegate void TimingPointHoldingEventHandler(TimingPoint timingPoint);

    public static Signals Instance = null!;

    public override void _Ready() => Instance = this;
}