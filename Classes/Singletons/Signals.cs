using System;
using Godot;
using SysCol = System.Collections.Generic;

namespace Tempora.Classes.Utility;

/// <summary>
///     Singleton class that sends signals to keep everything updated when properties change.
/// </summary>
public partial class Signals : Node
{
    public event EventHandler AudioFileChanged = null!;
    public event EventHandler MouseLeftReleased = null!;
    public event EventHandler Scrolled = null!;
    public event EventHandler SelectedPositionChanged = null!;
    public event EventHandler SettingsChanged = null!;
    public event EventHandler TimingChanged = null!;
    public event EventHandler TimingPointHolding = null!;
    public event EventHandler MusicPositionChangeRejected = null!;


    public class FloatArgument(float value) : EventArgs
    {
        private float value = value;
        public float Value
        {
            get => value;
        }
    }

    /// <summary>
    /// Simple <see cref="EventArgs"/> class which contains a single <see cref="ObjectArgument{T}.Value"/> property which can be accessed by the event subscriber.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    public class ObjectArgument<T>(T value) : EventArgs
    {
        private T value = value;
        public T Value
        {
            get => value;
        }
    }

    public enum Events
    {
        AudioFileChanged,
        MouseLeftReleased,
        Scrolled,
        SelectedPositionChanged,
        SettingsChanged,
        TimingChanged,
        TimingPointHolding,
        MusicPositionChangeRejected
    }

    private SysCol.Dictionary<Events, EventHandler> EventsDict = null!;

    public void EmitEvent(Events events) => EventsDict[events]?.Invoke(this, EventArgs.Empty);
    public void EmitEvent(Events events, EventArgs e) => EventsDict[events]?.Invoke(this, e);

    private static Signals instance = null!;

    public static Signals Instance { get => instance; set => instance = value; }

    public override void _Ready()
    {
        Instance = this;

        EventsDict = new SysCol.Dictionary<Events, EventHandler>
        {
            { Events.AudioFileChanged, (s, e) => AudioFileChanged?.Invoke(s, e) },
            { Events.MouseLeftReleased, (s, e) => MouseLeftReleased?.Invoke(s, e) },
            { Events.Scrolled, (s, e) => Scrolled?.Invoke(s, e) },
            { Events.SelectedPositionChanged, (s, e) => SelectedPositionChanged?.Invoke(s, e) },
            { Events.SettingsChanged, (s, e) => SettingsChanged?.Invoke(s, e) },
            { Events.TimingChanged, (s, e) => TimingChanged?.Invoke(s, e) },
            { Events.TimingPointHolding, (s, e) => TimingPointHolding?.Invoke(s, e) },
            { Events.MusicPositionChangeRejected, (s, e) => MusicPositionChangeRejected?.Invoke(s, e) }
        };
    }
}