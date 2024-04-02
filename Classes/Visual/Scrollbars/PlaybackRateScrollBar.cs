using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class PlaybackRateScrollBar : LabeledScrollbarHorizontal
{
    private AudioPlayer audioPlayer = null!;

    public override void _Ready()
    {
        base._Ready();
        audioPlayer = Project.Instance.SongPlayer;
    }

    protected override void UpdateValueLabel() => valueLabel.Text = (hScrollBar.Value * 100).ToString("0") + " %";

    protected override void UpdateReferenceValue()
    {
        if (audioPlayer == null)
            return;
        audioPlayer.PitchScale = (float)hScrollBar.Value;
    }

    protected override void SetInitialValue() => hScrollBar.Value = 1;
}