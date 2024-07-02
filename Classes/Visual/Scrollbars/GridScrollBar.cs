using System.Collections.Generic;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class GridScrollBar : LabeledScrollbarHorizontal
{
    protected static readonly Dictionary<int, string> GridDivisorToLabelDict = new() {
        { 1, "4/4" },
        { 2, "2/4" },
        { 3, "1/3" },
        { 4, "1/4" },
        { 6, "1/6" },
        { 8, "1/8" },
        { 12, "1/12" },
        { 16, "1/16" }
    };

    protected override void UpdateValueLabel()
    {
        var divisor = Settings.GridSliderToDivisorDict[(int)hScrollBar.Value];
        var label = GridDivisorToLabelDict[divisor];
        valueLabel.Text = label;
    }

    protected override void UpdateTarget() => Settings.Instance.GridDivisor = Settings.GridSliderToDivisorDict[(int)hScrollBar.Value];

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.GridDivisor;
}