using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class OverlapScrollBar : LabeledScrollbarHorizontal
{
    protected override void UpdateValueLabel() => valueLabel.Text = (hScrollBar.Value * 100).ToString("0") + " %";

    protected override void UpdateReferenceValue() => Settings.Instance.MusicPositionMargin = (float)hScrollBar.Value;

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.MusicPositionMargin;
}