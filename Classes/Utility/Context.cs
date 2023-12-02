using Godot;

namespace OsuTimer.Classes.Utility;

/// <summary>
///     Manages user context, such as object selection
/// </summary>
public partial class Context : Node {
    public static Context Instance;

    public TimingPoint HeldTimingPoint;

    public bool IsSelectedMusicPositionMoving = false;

    private float selectedPosition;

    public float SelectedMusicPosition {
        get => selectedPosition;
        set {
            if (value == selectedPosition) return;
            selectedPosition = value;
            Signals.Instance.EmitSignal("SelectedPositionChanged");
        }
    }

    public override void _Ready() {
        Instance = this;

        Signals.Instance.TimingPointHolding += OnTimingPointHolding;
        Signals.Instance.MouseLeftReleased += OnMouseLeftReleased;
    }

    public void OnTimingPointHolding(TimingPoint timingPoint) {
        HeldTimingPoint = timingPoint;
    }

    public void OnMouseLeftReleased() {
        HeldTimingPoint = null;
    }
}