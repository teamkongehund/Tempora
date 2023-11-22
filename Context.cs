using Godot;
using System;

/// <summary>
/// Manages user context, such as object selection
/// </summary>
public partial class Context : Node
{
    public static Context Instance;

    public bool IsSelectedMusicPositionMoving = false;

    private float _selectedPosition;
    public float SelectedMusicPosition
    {
        get => _selectedPosition;
        set
        {
            if (value == _selectedPosition) return;
            _selectedPosition = value;
            Signals.Instance.EmitSignal("SelectedPositionChanged");
        }
    }

    public TimingPoint HeldTimingPoint;

    public override void _Ready()
    {
        Instance = this;

        Signals.Instance.TimingPointHolding += OnTimingPointHolding;
        Signals.Instance.MouseLeftReleased += OnMouseLeftReleased;
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
