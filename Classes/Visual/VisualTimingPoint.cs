using Godot;
using System;

public partial class VisualTimingPoint : Node2D
{
	public TimingPoint TimingPoint;

	public Label NumberLabel;
	public Label BPMLabel;

    Area2D Area2D;
	CollisionShape2D CollisionShape2D;

	public ulong SystemTimeWhenCreated;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Area2D = GetNode<Area2D>("Area2D");
		CollisionShape2D = Area2D.GetNode<CollisionShape2D>("CollisionShape2D");
		NumberLabel = GetNode<Label>("NumberLabel");
        BPMLabel = GetNode<Label>("BPMLabel");

        NumberLabel.Text = Timing.Instance.TimingPoints.FindIndex(point => point == TimingPoint).ToString();
        BPMLabel.Text = TimingPoint.BPM.ToString("0.00");

        SystemTimeWhenCreated = Time.GetTicksMsec();

		TimingPoint.Changed += OnTimingPointChanged;
    }

    public void OnTimingPointChanged(TimingPoint timingPoint)
	{
		UpdateLabels(timingPoint);
	}

	public void UpdateLabels(TimingPoint timingPoint)
	{
        NumberLabel.Text = Timing.Instance.TimingPoints.FindIndex(point => point == timingPoint).ToString();
        BPMLabel.Text = timingPoint.BPM.ToString("0.00");
    }

    public override void _ExitTree()
    {
        TimingPoint.Changed -= OnTimingPointChanged; // Necessary due to Godot bug
    }

    public override void _Input(InputEvent @event)
    {
		if (!Visible) return;
        Vector2 mousePosition = GetLocalMousePosition();
        Rect2 rectangle = (Rect2)CollisionShape2D.Shape.GetRect();
        bool hasMouseInside = rectangle.HasPoint(mousePosition);
        if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && hasMouseInside)
			{
				GD.Print($"Clicked on TimingPoint with BPM {TimingPoint.BPM} & Time signature {TimingPoint.TimeSignature[0]}/{TimingPoint.TimeSignature[1]}");
				Signals.Instance.EmitSignal("TimingPointHolding", TimingPoint);
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
			{
				Signals.Instance.EmitSignal("MouseLeftReleased");
				return;
			}
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && hasMouseInside && !Input.IsKeyPressed(Key.Alt))
            {
				Context.Instance.HeldTimingPoint = null;

				// Prevent accidental deletion un inadvertent double-double-clicking
				if (Time.GetTicksMsec() - SystemTimeWhenCreated > 500)
				{
					TimingPoint.Delete();
				}
				else
				{
                    Signals.Instance.EmitSignal("TimingPointHolding", TimingPoint);
                }

				Viewport viewport = GetViewport();
				viewport.SetInputAsHandled();
				return;
            }
			if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl) && hasMouseInside)
			{
				// Decrease BPM by 1 (snapping to integers) - only for last timing point.
				float previousBPM = TimingPoint.BPM;
				float newBPM = (previousBPM % 1 == 0) ? previousBPM - 1 : (int) previousBPM;
				if (Input.IsKeyPressed(Key.Shift)) newBPM -= 4;

				TimingPoint.BPM_Update(newBPM);
			}
			else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl) && hasMouseInside)
            {
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBPM = TimingPoint.BPM;
                float newBPM = (int) previousBPM + 1;
                if (Input.IsKeyPressed(Key.Shift)) newBPM += 4;

                TimingPoint.BPM_Update(newBPM);
            }
        }
	}

    public VisualTimingPoint(TimingPoint timingPoint)
	{
		TimingPoint = timingPoint;
	}

	public VisualTimingPoint() { }
}
