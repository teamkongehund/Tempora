using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class GridScrollBar : LabeledScrollbar
{
    [Export] private AudioPlayer audioPlayer = null!;

    protected override void UpdateValueLabel() => valueLabel.Text = hScrollBar.Value.ToString();

    protected override void UpdateValue() => Settings.Instance.Divisor = (int)hScrollBar.Value;

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.Divisor;
}