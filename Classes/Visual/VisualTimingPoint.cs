using System;
using System.Collections.Generic;
using Godot;
using GD = Tempora.Classes.DataTools.GD;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class VisualTimingPoint : Node2D
{
    [Export]
    private Area2D area2D = null!;
    [Export]
    private Label bpmLabel = null!;
    [Export]
    private CollisionShape2D collisionShape2D = null!;
    [Export]
    private Label numberLabel = null!;
    [Export]
    private ColorRect colorRect = null!;
    [Export]
    private Timer flashTimer = null!;

    private Vector2 defaultSize = new Vector2(128, 128);
    private Vector2 LargerSize => defaultSize * 1.5f;
    private Color defaultColor = new Color("ff990096");
    private Color red = new Color("ff000096");

    //private ulong SystemTimeWhenCreated;

    private TimingPoint timingPoint = null!;
    public TimingPoint TimingPoint
    {
        get => timingPoint;
        set
        {
            if (timingPoint == value)
                return;
            timingPoint = value;
            SubscribeToTimingPointEvents();
        }
    }

    private void SubscribeToTimingPointEvents()
    {
        TimingPoint.Changed += OnTimingPointChanged;
        //TimingPoint.MusicPositionChangeRejected += OnMusicPositionChangeRejected;
    }

    public VisualTimingPoint(TimingPoint timingPoint)
    {
        TimingPoint = timingPoint;
    }

    public VisualTimingPoint() { }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        numberLabel.Text = Timing.Instance.TimingPoints.IndexOf(TimingPoint).ToString();
        bpmLabel.Text = TimingPoint.Bpm.ToString("0.00");

        //SystemTimeWhenCreated = Time.GetTicksMsec();

        SubscribeToTimingPointEvents();

        //VisibilityChanged += OnVisibilityChanged;

        Signals.Instance.MusicPositionChangeRejected += OnMusicPositionChangeRejected;

        flashTimer.Timeout += OnFlashTimerTimeout;
        defaultColor = colorRect.Color;
        defaultSize = colorRect.Size;
    }

    private void OnTimingPointChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            return;
        UpdateLabels(timingPoint);
    }

    //private void OnVisibilityChanged()
    //{
    //    //GD.Print("VisibilityChanged");
    //    if (Visible)
    //        SystemTimeWhenCreated = Time.GetTicksMsec();
    //}

    public void UpdateLabels(TimingPoint timingPoint)
    {
        numberLabel.Text = Timing.Instance.TimingPoints.IndexOf(timingPoint).ToString();
        bpmLabel.Text = timingPoint.Bpm.ToString("0.00");
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;
        Vector2 mousePosition = GetLocalMousePosition();
        Rect2 rectangle = collisionShape2D.Shape.GetRect();
        bool hasMouseInside = rectangle.HasPoint(mousePosition);

        if (@event is not InputEventMouseButton mouseEvent)
            return;
        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
        {
            Signals.Instance.EmitEvent(Signals.Events.MouseLeftReleased);
            return;
        }
        if (!hasMouseInside)
            return;

        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed == true)
        {
            GD.Print("Left click received");
            GD.Print($"mouseEvent.DoubleClick = {mouseEvent.DoubleClick}");
        }

        if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && !Input.IsKeyPressed(Key.Alt))
            DeleteTimingPoint();
        else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            Signals.Instance.EmitEvent(Signals.Events.TimingPointHolding, new Signals.ObjectArgument<TimingPoint>(TimingPoint));
        else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
        {
            // Decrease BPM by 1 (snapping to integers) - only for last timing point.
            float previousBpm = TimingPoint.Bpm;
            float newBpm = (int)previousBpm - 1;
            if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                newBpm = (int)previousBpm - 5;
            else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                newBpm = previousBpm - 0.1f;

            TimingPoint.Bpm_Set(newBpm, Timing.Instance);
        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl))
        {
            // Increase BPM by 1 (snapping to integers) - only for last timing point.
            float previousBpm = TimingPoint.Bpm;
            float newBpm = (int)previousBpm + 1;
            if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                newBpm = (int)previousBpm + 5;
            else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                newBpm = previousBpm + 0.1f;

            TimingPoint.Bpm_Set(newBpm, Timing.Instance);
        }
        else
            return;

        GetViewport().SetInputAsHandled();
    }

    private void DeleteTimingPoint()
    {
        GD.Print("DeleteTimingPoint() started");

        // Prevent accidental deletion un inadvertent double-double-clicking. Instead treated as holding the timing point
        if (Time.GetTicksMsec() - TimingPoint.SystemTimeWhenCreatedMsec <= 500)
        {
            GD.Print("Nah, we're actually holding the timing point");
            Signals.Instance.EmitEvent(Signals.Events.TimingPointHolding, new Signals.ObjectArgument<TimingPoint>(TimingPoint));
            return;
        }
        else
        {
            GD.Print("Now calling TimingPoint.Delete()");
            TimingPoint.Delete();
            //Context.Instance.HeldTimingPoint = null;
        }

        Viewport viewport = GetViewport();
        viewport.SetInputAsHandled();
        return;
    }

    private void OnFlashTimerTimeout()
    {
        if (colorRect.Color != defaultColor)
        {
            RevertFlash();
            flashTimer.Start(); // period of default color
        }
    }

    private void RevertFlash()
    {
        colorRect.Color = defaultColor;
        SetColorRectSize(defaultSize);
    }

    private void SetColorRectSize(Vector2 size)
    {
        colorRect.Size = size;
        colorRect.Position = -size / 2;
        colorRect.PivotOffset = size / 2;
    }

    private void OnMusicPositionChangeRejected(object? sender, EventArgs e)
    {
        if (e is not Signals.ObjectArgument<TimingPoint> timingPointArgument)
            return;
        if (timingPointArgument.Value != TimingPoint)
            return;
        FlashRed();
    }

    private void FlashRed()
    {
        //GD.Print($"VisualTimingPoint with {TimingPoint.MusicPosition}: Flashing Red!");
        if (!flashTimer.IsStopped())
            return;
        colorRect.Color = red;
        SetColorRectSize(LargerSize);
        flashTimer.Start();
    }
}