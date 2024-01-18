using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class BlockScrollBar : VScrollBar
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => Signals.Instance.TimingChanged += OnTimingChanged;

    public void OnTimingChanged()
    {
        if (Value != MaxValue) // Prevents inadvertent scrolling
            UpdateRange();
    }

    public void UpdateRange()
    {
        _ = Value;
        int firstMeasure = (int)Timing.Instance.TimeToMusicPosition(0);
        int lastMeasure = Timing.Instance.GetLastMeasure() - (Settings.Instance.NumberOfBlocks - 1);
        MinValue = firstMeasure;
        MaxValue = lastMeasure;
    }
}