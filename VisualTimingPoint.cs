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

        Area2D.InputEvent += OnArea2DInputEvent;
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
				Viewport viewport = GetViewport();
				viewport.SetInputAsHandled();
            }
        }
	}

    public VisualTimingPoint(TimingPoint timingPoint)
	{
		TimingPoint = timingPoint;
	}

	public VisualTimingPoint() { }

	public void OnArea2DInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		//if (@event is InputEventMouseButton mouseEvent)
		//{
		//	if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
		//	{
		//		GD.Print("VisualTimingPoint.OnArea2DInputEvent: Holding");
		//		Signals.Instance.EmitSignal("TimingPointHolding", TimingPoint);
		//	}
		//	else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
		//	{
		//		GD.Print("VisualTimingPoint.OnArea2DInputEvent: MouseLeftReleased");
  //              Signals.Instance.EmitSignal("MouseLeftReleased");
  //          }
		//}
	}
}
