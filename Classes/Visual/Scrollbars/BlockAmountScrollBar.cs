using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class BlockAmountScrollBar : LabeledScrollbarHorizontal
{
    protected override void UpdateValueLabel() => valueLabel.Text = ((int)hScrollBar.Value).ToString();

    protected override void UpdateTarget() => Settings.Instance.NumberOfBlocks = (int)hScrollBar.Value;

    protected override void SetInitialValue() =>
        //GD.Print(Settings.Instance.NumberOfBlocks);
        hScrollBar.Value = Settings.Instance.NumberOfBlocks;
}