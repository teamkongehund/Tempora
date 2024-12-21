using Godot;
using System;
using Tempora.Classes.Utility;
using Tempora.Classes.Visual;

public partial class FontSizeScrollBar : LabeledScrollbarHorizontal
{
    protected override void UpdateValueLabel() => valueLabel.Text = (hScrollBar.Value).ToString("0");

    protected override void UpdateTarget() => Settings.Instance.FontSize = (int)hScrollBar.Value;

    protected override void SetInitialValue() => hScrollBar.Value = Settings.Instance.FontSize;
}
