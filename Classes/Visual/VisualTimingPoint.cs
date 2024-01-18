using System;
using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

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
    }

    public void OnTimingPointChanged(object? sender, EventArgs e)
    {
        if (sender is not TimingPoint timingPoint)
            return;
        UpdateLabels(timingPoint);
    }

    public void UpdateLabels(TimingPoint timingPoint)
    {
        NumberLabel.Text = Timing.Instance.TimingPoints.IndexOf(timingPoint).ToString();
        BpmLabel.Text = timingPoint.Bpm.ToString("0.00");
    }

    public override void _ExitTree() => TimingPoint.Changed -= OnTimingPointChanged; // Necessary due to Godot bug

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;
        Vector2 mousePosition = GetLocalMousePosition();
        Rect2 rectangle = collisionShape2D.Shape.GetRect();
        bool hasMouseInside = rectangle.HasPoint(mousePosition);
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && hasMouseInside)
            {
                //GD.Print($"Clicked on TimingPoint with BPM {TimingPoint.Bpm} & Time signature {TimingPoint.TimeSignature[0]}/{TimingPoint.TimeSignature[1]}");

                Signals.Instance.EmitEvent(Signals.Events.TimingPointHolding, new Signals.TimingPointArgument(TimingPoint));
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
            {
                Signals.Instance.EmitEvent(Signals.Events.MouseLeftReleased);
                return;
            }

            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && hasMouseInside && !Input.IsKeyPressed(Key.Alt))
            {
                DeleteTimingPoint();
            }

            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl) && hasMouseInside)
            {
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBpm = TimingPoint.Bpm;
                float newBpm = (int)previousBpm - 1;
                if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                    newBpm = (int)previousBpm - 5;
                else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                    newBpm = previousBpm - 0.1f;

                TimingPoint.Bpm = newBpm;
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl) && hasMouseInside)
            {
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBpm = TimingPoint.Bpm;
                float newBpm = (int)previousBpm + 1;
                if (Input.IsKeyPressed(Key.Shift) && !Input.IsKeyPressed(Key.Alt))
                    newBpm = (int)previousBpm + 5;
                else if (!Input.IsKeyPressed(Key.Shift) && Input.IsKeyPressed(Key.Alt))
                    newBpm = previousBpm + 0.1f;

                TimingPoint.Bpm = newBpm;
            }
        }
    }

    private void DeleteTimingPoint()
    {
        Context.Instance.HeldTimingPoint = null;

        // Prevent accidental deletion un inadvertent double-double-clicking
        if (Time.GetTicksMsec() - SystemTimeWhenCreated > 500)
            TimingPoint.Delete();
        else
            Signals.Instance.EmitEvent(Signals.Events.TimingPointHolding, new Signals.TimingPointArgument(TimingPoint));

        Viewport viewport = GetViewport();
        viewport.SetInputAsHandled();
        return;
    }
}