using Godot;
using System;
using GD = Tempora.Classes.DataHelpers.GD;

public partial class LoopTimer : Node
{
    [Export]
    Timer InitialTimer = null!;

    [Export]
    Timer QuickTimer = null!;

    public event EventHandler? TimeOut = null;

    private double initialDelay = 0.5;

    private double loopLength = 0.075;

    public double LoopLength
    {
        get => loopLength;
        set
        {
            loopLength = value;
            QuickTimer.WaitTime = value;
        }
    }

    public double InitialDelay
    {
        get => initialDelay;
        set
        {
            initialDelay = value;
            InitialTimer.WaitTime = value;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        InitialTimer.WaitTime = InitialDelay;
        QuickTimer.WaitTime = LoopLength;
        InitialTimer.Timeout += OnInitiateTimerTimeout;
        QuickTimer.Timeout += OnQuickTimerTimeout;
    }

    public void DelayedStart()
    {
        InitialTimer?.Start();
    }

    public void Start()
    {
        QuickTimer?.Start();
    }

    public void Stop()
    {
        InitialTimer?.Stop();
        QuickTimer?.Stop();
    }

    private void OnInitiateTimerTimeout()
    {
        QuickTimer.Start();
    }

    private void OnQuickTimerTimeout()
    {
        TimeOut?.Invoke(this, EventArgs.Empty);
    }
}
