using System;
using System.Reflection;
using Godot;
using Tempora.Classes.TimingClasses;
using SysCol = System.Collections.Generic;

namespace Tempora.Classes.Utility;

/// <summary>
///     Singleton event aggregator allowing any class to subscribe to or invoke its events. 
/// </summary>
public partial class GlobalEvents : Node
{
    private static GlobalEvents instance = null!;
    public static GlobalEvents Instance { get => instance; private set => instance = value; }

    public event EventHandler? AudioFileChanged;
    public event EventHandler? MouseLeftReleased;
    public event EventHandler? Scrolled;
    public event EventHandler? SelectedPositionChanged;
    public event EventHandler? SettingsChanged;
    public event EventHandler? TimingChanged;
    public event EventHandler? TimingPointCountChanged;
    public event EventHandler? TimingPointHolding;
    /// <summary>
    /// Event argument should be an <see cref="ObjectArgument{TimingPoint}"/> of the point rejecting the change
    /// </summary>
    public event EventHandler? MusicPositionChangeRejected;
    public event EventHandler? TimingPointNearestCursorChanged;
    public event EventHandler? ContextMenuRequested;
    public event EventHandler? TimingPointAdded;

    /// <summary>
    /// Allows any class to invoke any event defined in <see cref="GlobalEvents"/>
    /// </summary>
    public void InvokeEvent(string eventName, object sender, EventArgs e)
    {
        EventHandler? eventHandler = GetEventHandler(eventName);
        eventHandler?.Invoke(sender, e);
    }

    public void InvokeEvent(string eventName) => InvokeEvent(eventName, this, EventArgs.Empty);

    //public void InvokeEvent(string eventName, object sender) => InvokeEvent(eventName, sender, EventArgs.Empty);

    public void InvokeEvent(string eventName, EventArgs args) => InvokeEvent(eventName, this, args);

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

    /// <summary>
    /// Use Reflection to find the relevant EventHandler object.
    /// </summary>
    private EventHandler? GetEventHandler(string eventName)
    {
        EventInfo? eventInfo = GetType().GetEvent(eventName);
        if (eventInfo != null)
        {
            Delegate? eventDelegate = (Delegate?)GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(this);
            if (eventDelegate != null)
            {
                return (EventHandler)eventDelegate;
            }
        }
        return null;
    }

    public override void _Ready()
    {
        Instance = this;

        TimingPointCountChanged += InvokeTimingChanged;
    }

    private void InvokeTimingChanged(object? sender, EventArgs e) => TimingChanged?.Invoke(sender, e);
}