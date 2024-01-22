using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class BlockAmountScrollBar : LabeledScrollbar
{
    protected override void UpdateValueLabel() => valueLabel.Text = ((int)hScrollBar.Value).ToString();

    protected override void UpdateValue() => Settings.Instance.NumberOfBlocks = (int)hScrollBar.Value;

    protected override void SetInitialValue() =>
        //GD.Print(Settings.Instance.NumberOfBlocks);
        hScrollBar.Value = Settings.Instance.NumberOfBlocks;
}