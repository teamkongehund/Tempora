using System;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class VisualTimingPoint : Node2D
{
    private Area2D area2D = null!;
    public Label BpmLabel = null!;
    private CollisionShape2D collisionShape2D = null!;

    public Label NumberLabel = null!;

    private ulong SystemTimeWhenCreated;
    public TimingPoint TimingPoint = null!;

    public VisualTimingPoint(TimingPoint timingPoint)
    {
        TimingPoint = timingPoint;
    }

    public VisualTimingPoint() { }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        area2D = GetNode<Area2D>("Area2D");
        collisionShape2D = area2D.GetNode<CollisionShape2D>("CollisionShape2D");
        NumberLabel = GetNode<Label>("NumberLabel");
        BpmLabel = GetNode<Label>("BPMLabel");

        NumberLabel.Text = Timing.Instance.TimingPoints.IndexOf(TimingPoint).ToString();
        BpmLabel.Text = TimingPoint.Bpm.ToString("0.00");

        SystemTimeWhenCreated = Time.GetTicksMsec();

        TimingPoint.Changed += OnTimingPointChanged;

        VisibilityChanged += OnVisibilityChanged;
    }

    private void OnTimingPointChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            return;
        UpdateLabels(timingPoint);
    }

    private void OnVisibilityChanged()
    {
        if (Visible)
            SystemTimeWhenCreated = Time.GetTicksMsec();
    }

    public void UpdateLabels(TimingPoint timingPoint)
    {
        NumberLabel.Text = Timing.Instance.TimingPoints.IndexOf(timingPoint).ToString();
        BpmLabel.Text = timingPoint.Bpm.ToString("0.00");
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

    //private void IncrementTimingPointBpm(InputEventMouseButton mouseEvent)
    //{
    //    if (!mouseEvent.Pressed || )
    //        return;
    //    int direction = mouseEvent.ButtonIndex == MouseButton.WheelUp

    //    float previousBpm = TimingPoint.Bpm;
    //    float newBpm = (int)previousBpm - 1;
    //    if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
    //        newBpm = (int)previousBpm - 5;
    //    else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
    //        newBpm = previousBpm - 0.1f;

    //    TimingPoint.Bpm_Set(newBpm, Timing.Instance);
    //}

    private void DeleteTimingPoint()
    {
        // Prevent accidental deletion un inadvertent double-double-clicking. Instead treated as holding the timing point
        if (Time.GetTicksMsec() - SystemTimeWhenCreated <= 500)
        {
            Signals.Instance.EmitEvent(Signals.Events.TimingPointHolding, new Signals.ObjectArgument<TimingPoint>(TimingPoint));
            return;
        }
        else
        {
            TimingPoint.Delete();
            //Context.Instance.HeldTimingPoint = null;
        }

        Viewport viewport = GetViewport();
        viewport.SetInputAsHandled();
        return;
    }
}