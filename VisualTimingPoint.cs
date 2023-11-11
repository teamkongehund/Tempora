using Godot;
using System;

public partial class VisualTimingPoint : Node2D
{
	public TimingPoint TimingPoint;

    Area2D Area2D;
	CollisionShape2D CollisionShape2D;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Area2D = GetNode<Area2D>("Area2D");
		CollisionShape2D = Area2D.GetNode<CollisionShape2D>("CollisionShape2D");
	}

    public override void _Input(InputEvent @event)
    {
        Vector2 mousePosition = GetLocalMousePosition();
        Rect2 rectangle = (Rect2)CollisionShape2D.Shape.GetRect();
        bool hasMouseInside = rectangle.HasPoint(mousePosition);
        if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && hasMouseInside)
			{
				Signals.Instance.EmitSignal("TimingPointHolding", TimingPoint);
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
			{
				Signals.Instance.EmitSignal("MouseLeftReleased");
			}
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && hasMouseInside)
            {
				Signals.Instance.HeldTimingPoint = null;

				TimingPoint.Delete();

				Viewport viewport = GetViewport();
				viewport.SetInputAsHandled();

				QueueFree();
            }
        }
	}

    public VisualTimingPoint(TimingPoint timingPoint)
	{
		TimingPoint = timingPoint;
	}

	public VisualTimingPoint() { }
}
