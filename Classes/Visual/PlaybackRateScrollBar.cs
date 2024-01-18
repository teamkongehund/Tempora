using Godot;
using OsuTimer.Classes.Audio;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class PlaybackRateScrollBar : LabeledScrollbar
{
    [Export] AudioPlayer audioPlayer;

    protected override void UpdateValueLabel()
    {
        valueLabel.Text = (hScrollBar.Value * 100).ToString("0") +" %";
    }

    protected override void UpdateValue()
    {
        audioPlayer.PitchScale = (float)hScrollBar.Value;
    }

    protected override void SetInitialValue()
    {
        hScrollBar.Value = 1;
    }
}