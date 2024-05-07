using Godot;
using Tempora.Classes.Audio;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class GridScrollBar : LabeledScrollbarHorizontal
{
    protected override void UpdateValueLabel() => valueLabel.Text = Settings.GridSliderToDivisorDict[(int)hScrollBar.Value].ToString();

    protected override void UpdateValue() => Settings.Instance.GridDivisor = Settings.GridSliderToDivisorDict[(int)hScrollBar.Value];

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.GridDivisor;
}