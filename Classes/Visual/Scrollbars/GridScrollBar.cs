using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class GridScrollBar : LabeledScrollbarHorizontal
{
    [Export] private AudioPlayer audioPlayer = null!;

    protected override void UpdateValueLabel() => valueLabel.Text = Settings.SliderToDivisorDict[(int)hScrollBar.Value].ToString();

    protected override void UpdateValue() => Settings.Instance.GridDivisor = Settings.SliderToDivisorDict[(int)hScrollBar.Value];

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.GridDivisor;
}