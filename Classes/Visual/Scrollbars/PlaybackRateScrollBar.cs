using Godot;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Visual;

public partial class PlaybackRateScrollBar : LabeledScrollbarHorizontal
{
    private MusicPlayer MusicPlayer => MusicPlayer.Instance;

    public override void _Ready()
    {
        base._Ready();

        MusicPlayer.Instance.PitchScaleChanged += OnPitchScaleChanged;
    }

    protected override void UpdateLabel() => valueLabel.Text = (hScrollBar.Value * 100).ToString("0") + " %";

    protected override void UpdateTarget()
    {
        MusicPlayer.SetPitchScale((float)hScrollBar.Value);
    }

    protected override void SetInitialValue() => hScrollBar.Value = 1;

    private void OnPitchScaleChanged(float pitchScale) => UpdateValue(pitchScale);

    /// <summary>
    /// Used to manually update the scrollbar from a different class, like <see cref="MusicPlayer"/>.
    /// </summary>
    private void UpdateValue(double pitchScale)
    {
        hScrollBar.Value = pitchScale;
        UpdateLabel();
    }
}