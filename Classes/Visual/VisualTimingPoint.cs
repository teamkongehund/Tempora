using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class VisualTimingPoint : Node2D {
    private Area2D area2D;
    public Label BpmLabel;
    private CollisionShape2D collisionShape2D;

    public Label NumberLabel;

    public ulong SystemTimeWhenCreated;
    public TimingPoint TimingPoint;

    public VisualTimingPoint(TimingPoint timingPoint) {
        TimingPoint = timingPoint;
    }

    public VisualTimingPoint() { }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        area2D = GetNode<Area2D>("Area2D");
        collisionShape2D = area2D.GetNode<CollisionShape2D>("CollisionShape2D");
        NumberLabel = GetNode<Label>("NumberLabel");
        BpmLabel = GetNode<Label>("BPMLabel");

        NumberLabel.Text = Timing.Instance.TimingPoints.FindIndex(point => point == TimingPoint).ToString();
        BpmLabel.Text = TimingPoint.Bpm.ToString("0.00");

        SystemTimeWhenCreated = Time.GetTicksMsec();

        TimingPoint.Changed += OnTimingPointChanged;
    }

    public void OnTimingPointChanged(TimingPoint timingPoint) {
        UpdateLabels(timingPoint);
    }

    public void UpdateLabels(TimingPoint timingPoint) {
        NumberLabel.Text = Timing.Instance.TimingPoints.FindIndex(point => point == timingPoint).ToString();
        BpmLabel.Text = timingPoint.Bpm.ToString("0.00");
    }

    public override void _ExitTree() {
        TimingPoint.Changed -= OnTimingPointChanged; // Necessary due to Godot bug
    }

    public override void _Input(InputEvent @event) {
        if (!Visible) return;
        var mousePosition = GetLocalMousePosition();
        var rectangle = collisionShape2D.Shape.GetRect();
        bool hasMouseInside = rectangle.HasPoint(mousePosition);
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed && hasMouseInside) {
                Gd.Print($"Clicked on TimingPoint with BPM {TimingPoint.Bpm} & Time signature {TimingPoint.TimeSignature[0]}/{TimingPoint.TimeSignature[1]}");
                Signals.Instance.EmitSignal("TimingPointHolding", TimingPoint);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased()) {
                Signals.Instance.EmitSignal("MouseLeftReleased");
                return;
            }

            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.DoubleClick && hasMouseInside && !Input.IsKeyPressed(Key.Alt)) {
                Context.Instance.HeldTimingPoint = null;

                // Prevent accidental deletion un inadvertent double-double-clicking
                if (Time.GetTicksMsec() - SystemTimeWhenCreated > 500)
                    TimingPoint.Delete();
                else
                    Signals.Instance.EmitSignal("TimingPointHolding", TimingPoint);

                var viewport = GetViewport();
                viewport.SetInputAsHandled();
                return;
            }

            if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl) && hasMouseInside) {
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBpm = TimingPoint.Bpm;
                float newBpm = previousBpm % 1 == 0 ? previousBpm - 1 : (int)previousBpm;
                if (Input.IsKeyPressed(Key.Shift)) newBpm -= 4;

                TimingPoint.BPM_Update(newBpm);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed && Input.IsKeyPressed(Key.Ctrl) && hasMouseInside) {
                // Decrease BPM by 1 (snapping to integers) - only for last timing point.
                float previousBpm = TimingPoint.Bpm;
                float newBpm = (int)previousBpm + 1;
                if (Input.IsKeyPressed(Key.Shift)) newBpm += 4;

                TimingPoint.BPM_Update(newBpm);
            }
        }
    }
}