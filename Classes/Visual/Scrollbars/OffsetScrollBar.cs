using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class OffsetScrollBar : LabeledScrollbar
{
    protected override void UpdateValueLabel() => valueLabel.Text = (hScrollBar.Value * 100).ToString("0.0") + " %";

    protected override void UpdateValue() => Settings.Instance.MusicPositionOffset = (float)hScrollBar.Value;

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.MusicPositionOffset;
}