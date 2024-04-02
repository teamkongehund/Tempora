using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class BlockAmountScrollBar : LabeledScrollbarHorizontal
{
    protected override void UpdateValueLabel() => valueLabel.Text = ((int)hScrollBar.Value).ToString();

    protected override void UpdateReferenceValue() => Settings.Instance.NumberOfRows = (int)hScrollBar.Value;

    protected override void SetInitialValue() =>
        //GD.Print(Settings.Instance.NumberOfBlocks);
        hScrollBar.Value = Settings.Instance.NumberOfRows;

    public override void _Ready()
    {
        base._Ready();
        hScrollBar.MaxValue = Settings.Instance.MaxNumberOfRows;
        hScrollBar.Value = Settings.Instance.NumberOfRows;
    }
}